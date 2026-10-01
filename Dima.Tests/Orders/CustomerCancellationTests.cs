using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Orders;

public class CustomerCancellationTests
{
    [Theory]
    [InlineData("open", 200)]
    [InlineData("blocked", 409)]
    [InlineData("unknown", 409)]
    [InlineData("expired", 409)]
    [InlineData("paid", 409)]
    [InlineData("other-user", 404)]
    public async Task Customer_cancellation_closes_checkout_before_releasing_order_and_voucher(string scenario, int code)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var clock = new FixedClock(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
        var user = new User { Id = 1, Email = "customer@test.com", UserName = "customer@test.com" };
        var product = new Product { Id = 1, Title = "Plan", Slug = "plan", Price = 100, AccessDurationMonths = 1 };
        var voucher = new Voucher { Id = 1, Code = "CANCEL", Value = 10 };
        var order = new Order
        {
            Id = 1, UserId = scenario == "other-user" ? 2 : 1,
            ProductId = 1, Product = product, VoucherId = 1, Voucher = voucher,
            PaymentSessionId = scenario == "unknown" ? null : "cs_open",
            PaymentSessionExpiresAt = clock.GetUtcNow().AddHours(1),
            ExpiresAt = clock.GetUtcNow().AddHours(scenario == "expired" ? -1 : 1),
            Status = scenario == "paid" ? EOrderStatus.Paid : EOrderStatus.WaitingPayment
        };
        var redemption = new VoucherRedemption
        {
            Id = 1, OrderId = 1, Order = order, VoucherId = 1, Voucher = voucher,
            UserId = order.UserId, Status = EVoucherRedemptionStatus.Reserved
        };
        db.AddRange(user, product, voucher, order, redemption);
        await db.SaveChangesAsync();
        var originalStatus = order.Status;
        var payment = new FakePaymentHandler { CloseSessionShouldSucceed = scenario != "blocked" };
        var handler = new OrderHandler(db, payment, new(db), Options.Create(new OrderExpirationOptions()),
            clock, TestBusinessTime.Create(clock));

        var result = await handler.CancelAsync(new CancelOrderRequest { Id = 1, UserId = user.Email! });

        Assert.Equal(code, result.Code);
        Assert.Equal(scenario is "open" or "blocked", payment.CloseSessionWasCalled);
        db.ChangeTracker.Clear();
        var savedOrder = await db.Orders.SingleAsync();
        var savedReservation = await db.VoucherRedemptions.SingleAsync();
        Assert.Equal(code == 200 ? EOrderStatus.Canceled : originalStatus, savedOrder.Status);
        Assert.Equal(code == 200 ? EVoucherRedemptionStatus.Released : EVoucherRedemptionStatus.Reserved, savedReservation.Status);
        if (code == 200)
        {
            Assert.Equal(EOrderStatus.Canceled, result.Data!.Status);
            Assert.Equal(clock.GetUtcNow().UtcDateTime, savedReservation.ReleasedAt);
        }
        else Assert.Null(savedReservation.ReleasedAt);
    }
}
