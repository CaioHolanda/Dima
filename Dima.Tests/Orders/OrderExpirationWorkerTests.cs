using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Models.Payments;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Orders;

public class OrderExpirationWorkerTests
{
    [Fact]
    public async Task Concurrent_payment_prevents_cancellation_and_voucher_release()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new AppDbContext(options);
        db.Orders.Add(new Order { Id = 1, PaymentSessionId = "cs_race", ExpiresAt = DateTimeOffset.UtcNow.AddHours(1) });
        db.VoucherRedemptions.Add(new Dima.Core.Models.Vouchers.VoucherRedemption
            { OrderId = 1, VoucherId = 1, Status = EVoucherRedemptionStatus.Reserved });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var payment = new PaymentStub
        {
            AllowClose = true,
            BeforeClose = async () =>
            {
                await using var other = new AppDbContext(options);
                var order = await other.Orders.SingleAsync();
                order.Status = EOrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
                // InMemory does not generate SQL Server rowversions automatically.
                order.RowVersion = [1];
                await other.SaveChangesAsync();
            }
        };
        var service = new OrderExpirationService(db, payment, TimeProvider.System, NullLogger<OrderExpirationService>.Instance);
        var result = await service.CancelAsync(1);
        Assert.Equal(409, result.Code);
        db.ChangeTracker.Clear();
        Assert.Equal(EOrderStatus.Paid, (await db.Orders.SingleAsync()).Status);
        Assert.Equal(EVoucherRedemptionStatus.Reserved, (await db.VoucherRedemptions.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("valid", 200)]
    [InlineData("paid", 409)]
    [InlineData("refunded", 409)]
    [InlineData("refund-pending", 409)]
    [InlineData("canceled", 409)]
    [InlineData("expired", 409)]
    [InlineData("overdue", 409)]
    [InlineData("legacy", 409)]
    [InlineData("unknown-session", 409)]
    [InlineData("paid-marker", 409)]
    [InlineData("access-marker", 409)]
    [InlineData("redeemed", 409)]
    [InlineData("checkout-blocked", 409)]
    [InlineData("checkout-open", 200)]
    public async Task Cancellation_checks_eligibility_and_preserves_reservation_on_failure(string scenario, int code)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var order = new Order { Id = 1, ExpiresAt = DateTimeOffset.UtcNow.AddHours(1), VoucherId = 1 };
        switch (scenario)
        {
            case "paid": order.Status = EOrderStatus.Paid; break;
            case "refunded": order.Status = EOrderStatus.Refunded; break;
            case "refund-pending": order.Status = EOrderStatus.RefundPending; break;
            case "canceled": order.Status = EOrderStatus.Canceled; break;
            case "expired": order.Status = EOrderStatus.Expired; break;
            case "overdue": order.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1); break;
            case "legacy": order.ExpiresAt = null; break;
            case "unknown-session": order.PaymentSessionExpiresAt = DateTimeOffset.UtcNow.AddHours(1); break;
            case "paid-marker": order.ExternalReference = "pi_paid"; break;
            case "access-marker": order.AccessStartsAt = DateTime.UtcNow; break;
            case "checkout-blocked": case "checkout-open": order.PaymentSessionId = "cs_test"; break;
        }
        var original = order.Status;
        var redemption = new Dima.Core.Models.Vouchers.VoucherRedemption
        {
            OrderId = 1, VoucherId = 1,
            Status = scenario == "redeemed" ? EVoucherRedemptionStatus.Redeemed : EVoucherRedemptionStatus.Reserved
        };
        db.AddRange(order, redemption);
        await db.SaveChangesAsync();
        var originalReservation = redemption.Status;
        var payment = new PaymentStub { AllowClose = scenario == "checkout-open" };
        var service = new OrderExpirationService(db, payment, TimeProvider.System, NullLogger<OrderExpirationService>.Instance);
        var result = await service.CancelAsync(1);
        Assert.Equal(code, result.Code);
        db.ChangeTracker.Clear();
        var saved = await db.Orders.SingleAsync();
        var reservation = await db.VoucherRedemptions.SingleAsync();
        Assert.Equal(code == 200 ? EOrderStatus.Canceled : original, saved.Status);
        Assert.Null(saved.ExpiredAt);
        Assert.Equal(code == 200 ? EVoucherRedemptionStatus.Released : originalReservation, reservation.Status);
        if (code == 200) Assert.NotNull(reservation.ReleasedAt);
        else Assert.Null(reservation.ReleasedAt);
    }

    [Fact]
    public async Task Sweep_expires_due_orders_across_batches_and_retries_blocked_checkout()
    {
        var now = DateTimeOffset.UtcNow;
        var payment = new PaymentStub();
        var services = new ServiceCollection();
        var database = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IPaymentHandler>(payment);
        services.AddDbContext<AppDbContext>(x => x.UseInMemoryDatabase(database));
        services.AddTransient<OrderExpirationService>();
        await using var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Orders.Add(new Order { Id = 1, ExpiresAt = now.AddMinutes(-1), PaymentSessionId = "blocked" });
            for (var id = 2; id <= 105; id++)
                db.Orders.Add(new Order { Id = id, ExpiresAt = now.AddMinutes(-1) });
            db.Orders.Add(new Order { Id = 106, ExpiresAt = now.AddHours(1) });
            db.Orders.Add(new Order { Id = 107, ExpiresAt = now.AddMinutes(-1), Status = EOrderStatus.Paid });
            db.Orders.Add(new Order { Id = 108 });
            await db.SaveChangesAsync();
        }
        var worker = new OrderExpirationWorker(provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System, Options.Create(new OrderExpirationOptions()), NullLogger<OrderExpirationWorker>.Instance);
        await worker.SweepAsync(default);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(104, await db.Orders.CountAsync(x => x.Status == EOrderStatus.Expired));
            Assert.Equal(EOrderStatus.WaitingPayment, (await db.Orders.FindAsync(1L))!.Status);
            Assert.Equal(EOrderStatus.WaitingPayment, (await db.Orders.FindAsync(106L))!.Status);
            Assert.Equal(EOrderStatus.Paid, (await db.Orders.FindAsync(107L))!.Status);
            Assert.Equal(EOrderStatus.WaitingPayment, (await db.Orders.FindAsync(108L))!.Status);
        }
        payment.AllowClose = true;
        await worker.SweepAsync(default);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(EOrderStatus.Expired, (await db.Orders.FindAsync(1L))!.Status);
        }
        Assert.Equal(2, payment.Calls);
        await Assert.ThrowsAsync<OperationCanceledException>(() => worker.SweepAsync(new CancellationToken(true)));
    }

    private sealed class PaymentStub : IPaymentHandler
    {
        public bool AllowClose { get; set; }
        public int Calls { get; private set; }
        public Func<Task>? BeforeClose { get; set; }
        public async Task<Response<bool>> CloseSessionAsync(string sessionId)
        {
            Calls++;
            if (BeforeClose is not null) await BeforeClose();
            return new Response<bool>(AllowClose, AllowClose ? 200 : 409, "checkout");
        }
        public Task<Response<PaymentSessionResult?>> CreateSessionAsync(CreatePaymentSessionRequest request)
            => throw new NotSupportedException();
        public Task<Response<string?>> RefundAsync(string externalReference, string idempotencyKey)
            => throw new NotSupportedException();
    }
}
