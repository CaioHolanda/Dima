using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dima.Api.Common.Api;
using WebhookEndpoint = Dima.Api.Endpoints.Stripe.WebhookEndpoint;
using Dima.Api.Observability;
using Dima.Core.Common;
using Dima.Core.Models;
using Dima.Core.Responses;
using Dima.Web.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stripe;
using Xunit;

namespace Dima.Tests.Observability;

public sealed class RequestObservabilityTests : IAsyncLifetime
{
    private readonly RecordingLoggerProvider _logs = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_logs);
        builder.Services.Configure<Dima.Api.Configuration.ApiOptions>(options => options.StripeWebhookSecret = "whsec_dt19_test");
        builder.Services.AddSingleton<IOrderPaymentConfirmationHandler, PaymentConfirmation>();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
            .WithOrigins("https://frontend.example").AllowAnyHeader().AllowAnyMethod()
            .WithExposedHeaders(RequestCorrelation.HeaderName)));
        _app = builder.Build();
        _app.UseMiddleware<RequestObservabilityMiddleware>();
        _app.UseCors();
        _app.MapGet("/ok", (ILogger<RequestObservabilityTests> logger) =>
        {
            logger.LogInformation("Handler executado");
            return Results.Ok();
        });
        _app.MapGet("/fail", () => Fail());
        WebhookEndpoint.Map(_app);
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    private static IResult Fail() => throw new InvalidOperationException("internal-secret-detail");

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task PreservesCorrelationAndScopesHandlerLogsWithoutLoggingQuery()
    {
        var id = Guid.NewGuid();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok?token=secret-query");
        request.Headers.Add(RequestCorrelation.HeaderName, id.ToString("D"));
        request.Headers.Add("Origin", "https://frontend.example");
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(id.ToString("N"), response.Headers.GetValues(RequestCorrelation.HeaderName).Single());
        Assert.Contains(RequestCorrelation.HeaderName,
            response.Headers.GetValues("Access-Control-Expose-Headers").Single());
        var entry = Assert.Single(_logs.Entries, x => x.Message == "Handler executado");
        Assert.Equal(id.ToString("N"), entry.Properties["CorrelationId"]);
        Assert.NotNull(entry.Properties["TraceId"]);
        Assert.Equal("/ok", entry.Properties["RequestPath"]);
        Assert.DoesNotContain(_logs.Entries, x => x.Message.Contains("secret-query"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("arbitrary-client-text")]
    [InlineData("00000000000000000000000000000000")]
    public async Task ReplacesInvalidCorrelation(string supplied)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        request.Headers.TryAddWithoutValidation(RequestCorrelation.HeaderName, supplied);
        using var response = await _client.SendAsync(request);
        var received = response.Headers.GetValues(RequestCorrelation.HeaderName).Single();
        Assert.Equal(received, RequestCorrelation.Normalize(received));
        Assert.NotEqual(supplied, received);
    }

    [Fact]
    public async Task ReplacesMultipleCorrelationHeaders()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ok");
        var first = RequestCorrelation.Create();
        request.Headers.Add(RequestCorrelation.HeaderName, new[] { first, RequestCorrelation.Create() });
        using var response = await _client.SendAsync(request);
        var received = response.Headers.GetValues(RequestCorrelation.HeaderName).Single();
        Assert.NotEqual(first, received);
        Assert.Equal(received, RequestCorrelation.Normalize(received));
    }

    [Fact]
    public async Task UnhandledExceptionReturnsGenericProblemAndLogsOriginalException()
    {
        using var response = await _client.GetAsync("/fail");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("internal-secret-detail", body);
        using var json = JsonDocument.Parse(body);
        var id = response.Headers.GetValues(RequestCorrelation.HeaderName).Single();
        Assert.Equal(id, json.RootElement.GetProperty("correlationId").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("traceId").GetString()));
        Assert.Contains(_logs.Entries, x => x.Level == LogLevel.Error
            && x.Exception?.Message == "internal-secret-detail"
            && Equals(x.Properties["CorrelationId"], id));
    }

    [Fact]
    public async Task FrontendGeneratesDistinctIdsAndLogsHttpFailureWithSameId()
    {
        using var client = new HttpClient(new RequestCorrelationHandler(
            _app.Services.GetRequiredService<ILogger<RequestCorrelationHandler>>())
        { InnerHandler = new HttpClientHandler() }) { BaseAddress = _client.BaseAddress };
        using var first = await client.GetAsync("/ok");
        using var second = await client.GetAsync("/fail");
        var firstId = first.Headers.GetValues(RequestCorrelation.HeaderName).Single();
        var secondId = second.Headers.GetValues(RequestCorrelation.HeaderName).Single();
        Assert.NotEqual(firstId, secondId);
        Assert.Contains(_logs.Entries, x => x.Level == LogLevel.Warning
            && x.Message.Contains("API retornou 500") && Equals(x.Properties["CorrelationId"], secondId));
    }

    [Fact]
    public async Task SignedWebhookCarriesEventPaymentAndOriginCorrelationIntoHandlerLogs()
    {
        const string secret = "whsec_dt19_test";

        var origin = RequestCorrelation.Create();
        var body = JsonSerializer.Serialize(new
        {
            id = "evt_dt19", @object = "event", type = "payment_intent.succeeded",
            api_version = StripeConfiguration.ApiVersion,
            data = new { @object = new
            {
                id = "pi_dt19", @object = "payment_intent", status = "succeeded",
                amount_received = 1000, currency = "brl",
                metadata = new Dictionary<string, string>
                { ["order"] = "ORDER019", ["userId"] = "1", [RequestCorrelation.StripeMetadataKey] = origin }
            } }
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhook")
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = Assert.Single(_logs.Entries, x => x.Message == "Confirmando pagamento no handler");
        Assert.Equal("evt_dt19", entry.Properties["EventId"]);
        Assert.Equal("pi_dt19", entry.Properties["PaymentIntentId"]);
        Assert.Equal("ORDER019", entry.Properties["OrderNumber"]);
        Assert.Equal(origin, entry.Properties["CheckoutCorrelationId"]);
        Assert.Equal(response.Headers.GetValues(RequestCorrelation.HeaderName).Single(), entry.Properties["CorrelationId"]);
    }

    private sealed class PaymentConfirmation(ILogger<PaymentConfirmation> logger) : IOrderPaymentConfirmationHandler
    {
        public Task<Response<Order?>> ConfirmPaymentAsync(string orderNumber, string externalReference,
            long amountReceived, string currency, string paymentUserId)
        {
            logger.LogInformation("Confirmando pagamento no handler");
            return Task.FromResult(new Response<Order?>(null));
        }

        public Task<Response<Order?>> ConfirmRefundAsync(string paymentIntentId, string refundId,
            string refundStatus, string? failureReason) => Task.FromResult(new Response<Order?>(null));
    }
}
