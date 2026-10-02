using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dima.Api.Common.Api;
using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Tests.Orders.Fakes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using WebhookEndpoint = Dima.Api.Endpoints.Stripe.WebhookEndpoint;

namespace Dima.Tests.Orders;

public sealed class RefundWebhookTests : IAsyncLifetime
{
    private const string Secret = "whsec_local_refund_tests";
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.Configure<ApiOptions>(options => options.StripeWebhookSecret = Secret);
        builder.Services.AddScoped<IOrderPaymentConfirmationHandler>(services =>
        {
            var db = services.GetRequiredService<AppDbContext>();
            return new OrderHandler(db, new FakePaymentHandler(), new(db), Options.Create(new OrderExpirationOptions()),
                TimeProvider.System, TestBusinessTime.Create(TimeProvider.System));
        });
        _app = builder.Build();
        WebhookEndpoint.Map(_app);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Orders.Add(new Order { Id = 1, ExternalReference = "pi_refund", RefundReference = "re_refund", Status = EOrderStatus.RefundPending });
            await db.SaveChangesAsync();
        }
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private async Task<HttpStatusCode> SendAsync(string type, string status, string id = "re_refund", string secret = Secret)
    {
        var body = JsonSerializer.Serialize(new
        {
            id = "evt_refund", @object = "event", type, api_version = StripeConfiguration.ApiVersion,
            data = new { @object = new { id, @object = "refund", payment_intent = "pi_refund", status,
                failure_reason = status == "failed" ? "declined" : (string?)null } }
        });
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret),
            Encoding.UTF8.GetBytes($"{timestamp}.{body}"))).ToLowerInvariant();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhook")
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        using var response = await _client.SendAsync(request);
        return response.StatusCode;
    }

    private async Task<Order> ReadOrderAsync()
    {
        using var scope = _app.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Orders.AsNoTracking().SingleAsync();
    }

    [Theory]
    [InlineData("refund.created", "succeeded", EOrderStatus.Refunded)]
    [InlineData("refund.created", "pending", EOrderStatus.RefundPending)]
    [InlineData("refund.updated", "succeeded", EOrderStatus.Refunded)]
    [InlineData("refund.failed", "failed", EOrderStatus.Paid)]
    public async Task Signed_events_persist_refund_state(string type, string status, EOrderStatus expected)
    {
        Assert.Equal(HttpStatusCode.OK, await SendAsync(type, status));
        Assert.Equal(expected, (await ReadOrderAsync()).Status);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("canceled")]
    public async Task Failure_after_success_clears_refunded_date_and_old_events_cannot_revive_refund(string failure)
    {
        Assert.Equal(HttpStatusCode.OK, await SendAsync("refund.created", "succeeded"));
        Assert.NotNull((await ReadOrderAsync()).RefundedAt);
        Assert.Equal(HttpStatusCode.OK, await SendAsync(failure == "failed" ? "refund.failed" : "refund.updated", failure));
        var failed = await ReadOrderAsync();
        Assert.Equal(EOrderStatus.Paid, failed.Status);
        Assert.Equal(failure == "failed" ? "declined" : "canceled", failed.RefundFailureReason);
        Assert.Null(failed.RefundedAt);
        foreach (var oldStatus in new[] { "pending", "requires_action", "succeeded", failure })
        {
            Assert.Equal(HttpStatusCode.OK, await SendAsync("refund.updated", oldStatus));
            var saved = await ReadOrderAsync();
            Assert.Equal(EOrderStatus.Paid, saved.Status);
            Assert.Equal(failed.RefundFailureReason, saved.RefundFailureReason);
            Assert.Equal(failed.UpdatedAt, saved.UpdatedAt);
            Assert.Null(saved.RefundedAt);
        }
    }

    [Fact]
    public async Task Duplicate_success_and_delayed_pending_preserve_completion()
    {
        await SendAsync("refund.created", "succeeded");
        var original = await ReadOrderAsync();
        foreach (var status in new[] { "succeeded", "pending", "requires_action" })
        {
            Assert.Equal(HttpStatusCode.OK, await SendAsync("refund.updated", status));
            var saved = await ReadOrderAsync();
            Assert.Equal(EOrderStatus.Refunded, saved.Status);
            Assert.Equal(original.RefundedAt, saved.RefundedAt);
            Assert.Equal(original.UpdatedAt, saved.UpdatedAt);
        }
    }

    [Fact]
    public async Task Invalid_signature_and_unrelated_refund_cannot_mutate_order()
    {
        Assert.Equal(HttpStatusCode.BadRequest, await SendAsync("refund.failed", "failed", secret: "wrong"));
        Assert.Equal(HttpStatusCode.Conflict, await SendAsync("refund.failed", "failed", id: "re_other"));
        Assert.Equal(EOrderStatus.RefundPending, (await ReadOrderAsync()).Status);
    }

    [Fact]
    public async Task Event_before_refund_reference_is_persisted_can_be_retried()
    {
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Orders.SingleAsync()).RefundReference = null;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, await SendAsync("refund.created", "succeeded"));
        Assert.Equal(EOrderStatus.RefundPending, (await ReadOrderAsync()).Status);
        using (var scope = _app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Orders.SingleAsync()).RefundReference = "re_refund";
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, await SendAsync("refund.created", "succeeded"));
        Assert.Equal(EOrderStatus.Refunded, (await ReadOrderAsync()).Status);
    }
}
