using System.Net;
using System.Text;
using System.Text.Json;
using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Common;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Requests.Payment;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Product = Dima.Core.Models.Product;
using Xunit;

namespace Dima.Tests.Observability;

public sealed class StripeCheckoutCorrelationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RetriedAttemptPreservesStripeParametersAndConnectsHttpLogs(bool legacy)
    {
        var expiration = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds());
        var transport = new StripeTransport(expiration, legacy);
        using var http = new HttpClient(transport);

        var stripeClient = new StripeClient("sk_test_dt19", httpClient:
            new SystemNetHttpClient(http, maxNetworkRetries: 0, enableTelemetry: false));
        var apiOptions = Options.Create(new ApiOptions { StripeApiKey = "sk_test_dt19" });
        var logs = new RecordingLoggerProvider();
        using var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var logger = factory.CreateLogger<StripePaymentHandler>();
        var requestIds = new[] { RequestCorrelation.Create(), RequestCorrelation.Create() };

        foreach (var id in requestIds)
        {
            // Two snapshots model retrying after Stripe succeeded but its session ID
            // was not persisted. The payment attempt and expiry remain unchanged.
            await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var product = new Product { Id = 1, Title = "Plano", Slug = "plano", Price = 10, AccessDurationMonths = 1 };
            db.Users.Add(new User { Id = 1, Email = "user@test.com", UserName = "user@test.com" });
            db.Products.Add(product);
            db.Orders.Add(new Order
            {
                Id = 19, Number = "ORDER019", UserId = 1, Product = product, ProductId = 1,
                OriginalPrice = 10, Total = 10, AccessDurationMonths = 1,
                Status = EOrderStatus.WaitingPayment, PaymentSessionExpiresAt = expiration,
                RowVersion = new byte[] { 1 }
            });
            await db.SaveChangesAsync();
            using var scope = logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = id });
            var result = await new StripePaymentHandler(db, TimeProvider.System,
                Options.Create(new OrderExpirationOptions()), apiOptions, stripeClient, logger).CreateSessionAsync(new CreatePaymentSessionRequest
                { OrderNumber = "ORDER019", UserId = "user@test.com" });
            Assert.True(result.Code == 200, $"Status {result.Code}: {result.Message}\n" +
                string.Join("\n", logs.Entries.Select(entry => entry.Exception?.ToString())));
            Assert.Equal("cs_dt19", result.Data!.SessionId);
        }

        Assert.Equal(legacy ? 4 : 2, transport.Calls.Count);
        Assert.Single(transport.Calls.Select(x => x.Key).Distinct());
        var successfulCalls = transport.Calls.Where(x => !legacy || !x.HasCorrelation).ToArray();
        Assert.Equal(successfulCalls[0].Body, successfulCalls[1].Body);
        var metadata = QueryHelpers.ParseQuery(transport.Calls[0].Body);
        var checkoutId = metadata["metadata[correlation_id]"].ToString();
        Assert.Equal(checkoutId, metadata["payment_intent_data[metadata][correlation_id]"].ToString());
        Assert.Equal(checkoutId, RequestCorrelation.Normalize(checkoutId));
        var entries = logs.Entries.Where(x => x.Properties.ContainsKey("CheckoutCorrelationId")).ToArray();
        Assert.Equal(2, entries.Length);
        Assert.All(entries, entry => Assert.Equal(checkoutId, entry.Properties["CheckoutCorrelationId"]));
        Assert.Equal(requestIds[0], entries[0].Properties["CorrelationId"]);
        Assert.Equal(requestIds[1], entries[1].Properties["CorrelationId"]);
        Assert.DoesNotContain(checkoutId, requestIds);
    }

    private sealed record StripeCall(string Key, string Body, bool HasCorrelation);
    private sealed class StripeTransport(DateTimeOffset expiration, bool legacy) : HttpMessageHandler
    {
        public List<StripeCall> Calls { get; } = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var rejected = false;
            if (request.Method == HttpMethod.Post)
            {
                Assert.Equal("/v1/checkout/sessions", request.RequestUri!.AbsolutePath);
                var body = await request.Content!.ReadAsStringAsync(cancellationToken);
                var hasCorrelation = QueryHelpers.ParseQuery(body).ContainsKey("metadata[correlation_id]");
                Calls.Add(new(request.Headers.GetValues("Idempotency-Key").Single(), body, hasCorrelation));
                rejected = legacy && hasCorrelation;
            }
            else
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("/v1/checkout/sessions/cs_dt19", request.RequestUri!.AbsolutePath);
            }
            var json = rejected
                ? JsonSerializer.Serialize(new { error = new { type = "idempotency_error", message = "Parameters differ from original attempt" } })
                : JsonSerializer.Serialize(new
                {
                    id = "cs_dt19", @object = "checkout.session", status = "open", payment_status = "unpaid",
                    url = "https://checkout.stripe.com/dt19", expires_at = expiration.ToUnixTimeSeconds()
                });
            return new HttpResponseMessage(rejected ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
            { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }
}
