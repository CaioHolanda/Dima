using Dima.Api.Data;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dima.Tests.Orders;

public class ScheduledOrderExpirationTests
{
    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ScheduledOrderExpirationProcessor Processor(AppDbContext db, RecordingExpirationScheduler scheduler)
        => new(db, new OrderExpirationService(db, new FakePaymentHandler(), new Clock(Now),
            NullLogger<OrderExpirationService>.Instance), scheduler, new Clock(Now));
    private static OrderExpirationMessage Message(Order order) => new(order.Id, order.Number, order.CreatedAt);

    [Fact]
    public async Task Due_message_expires_only_target_and_duplicate_is_safe()
    {
        await using var db = Database();
        var order = new Order { Id = 1, ExpiresAt = Now.AddMinutes(-1) };
        db.Orders.AddRange(order, new Order { Id = 2, ExpiresAt = Now.AddMinutes(-1) });
        db.VoucherRedemptions.Add(new() { OrderId = 1, VoucherId = 1, Status = EVoucherRedemptionStatus.Reserved });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var scheduler = new RecordingExpirationScheduler();
        await Processor(db, scheduler).ProcessAsync(Message(order));
        var expiredAt = (await db.Orders.FindAsync(1L))!.ExpiredAt;
        db.ChangeTracker.Clear();
        await Processor(db, scheduler).ProcessAsync(Message(order));
        Assert.Equal(EOrderStatus.Expired, (await db.Orders.FindAsync(1L))!.Status);
        Assert.Equal(expiredAt, (await db.Orders.FindAsync(1L))!.ExpiredAt);
        Assert.Equal(EOrderStatus.WaitingPayment, (await db.Orders.FindAsync(2L))!.Status);
        Assert.Equal(EVoucherRedemptionStatus.Released, (await db.VoucherRedemptions.SingleAsync()).Status);
        Assert.Empty(scheduler.Messages);
    }

    [Theory]
    [InlineData(EOrderStatus.Paid)]
    [InlineData(EOrderStatus.Canceled)]
    [InlineData(EOrderStatus.Expired)]
    [InlineData(EOrderStatus.RefundPending)]
    public async Task Terminal_orders_do_not_repeat_work(EOrderStatus status)
    {
        await using var db = Database();
        var order = new Order { Id = 1, ExpiresAt = Now.AddMinutes(-1), Status = status };
        db.Add(order);
        await db.SaveChangesAsync();
        var scheduler = new RecordingExpirationScheduler();
        await Processor(db, scheduler).ProcessAsync(Message(order));
        Assert.Equal(status, (await db.Orders.SingleAsync()).Status);
        Assert.Empty(scheduler.Messages);
    }

    [Fact]
    public async Task Extended_checkout_deadline_reschedules_without_expiring()
    {
        await using var db = Database();
        var order = new Order { Id = 1, ExpiresAt = Now.AddHours(1) };
        db.Add(order);
        await db.SaveChangesAsync();
        var scheduler = new RecordingExpirationScheduler();
        await Processor(db, scheduler).ProcessAsync(Message(order));
        Assert.Equal(order.ExpiresAt, Assert.Single(scheduler.Messages).DueAt);
        Assert.Equal(EOrderStatus.WaitingPayment, order.Status);
        scheduler.Fail = true;
        await Assert.ThrowsAsync<IOException>(() => Processor(db, scheduler).ProcessAsync(Message(order)));
    }

    [Fact]
    public async Task Missing_or_mismatched_orders_fail_for_durable_retry_not_acknowledgement()
    {
        await using var db = Database();
        var processor = Processor(db, new());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(new(1, "missing", Now.UtcDateTime)));
        var order = new Order { Id = 1, ExpiresAt = Now };
        db.Add(order);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            processor.ProcessAsync(new(1, "another", order.CreatedAt)));
        Assert.Equal(EOrderStatus.WaitingPayment, order.Status);
    }

    [Fact]
    public async Task Uncertain_payment_preserves_order_and_reservation_for_queue_retry()
    {
        await using var db = Database();
        var order = new Order { Id = 1, ExpiresAt = Now.AddMinutes(-1), PaymentSessionExpiresAt = Now };
        db.Add(order);
        db.VoucherRedemptions.Add(new() { OrderId = 1, VoucherId = 1, Status = EVoucherRedemptionStatus.Reserved });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Processor(db, new()).ProcessAsync(Message(order)));
        Assert.Equal(EOrderStatus.WaitingPayment, order.Status);
        Assert.Equal(EVoucherRedemptionStatus.Reserved, (await db.VoucherRedemptions.SingleAsync()).Status);
    }

    [Fact]
    public async Task Explicit_backfill_schedules_pending_orders_with_deadlines_only()
    {
        await using var db = Database();
        db.Orders.AddRange(new Order { Id = 1, ExpiresAt = Now },
            new Order { Id = 2, ExpiresAt = Now.AddHours(1) },
            new Order { Id = 3 },
            new Order { Id = 4, ExpiresAt = Now, Status = EOrderStatus.Paid });
        await db.SaveChangesAsync();
        var scheduler = new RecordingExpirationScheduler();
        Assert.Equal(2, await new OrderExpirationBackfill(db, scheduler).RunAsync());
        Assert.Equal(new long[] { 1, 2 }, scheduler.Messages.Select(x => x.Message.OrderId));
    }

    [Fact]
    public void Storage_visibility_is_bounded_without_losing_long_deadlines()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), QueueOrderExpirationScheduler.GetVisibilityDelay(Now.AddMinutes(-1), Now));
        Assert.Equal(TimeSpan.FromMinutes(30), QueueOrderExpirationScheduler.GetVisibilityDelay(Now.AddMinutes(30), Now));
        Assert.Equal(TimeSpan.FromDays(7), QueueOrderExpirationScheduler.GetVisibilityDelay(Now.AddDays(10), Now));
    }
}