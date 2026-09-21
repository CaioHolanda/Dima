using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Tests.Orders.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Orders;

public class OrderSchedulingTransactionTests
{
    private sealed class TestDb(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entity in builder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToList())
                    entity.RemoveCheckConstraint(constraint.Name!);
            builder.Entity<Order>().Property(x => x.Id).HasColumnType("INTEGER");
            builder.Entity<Order>().Property(x => x.RowVersion).ValueGeneratedNever();
        }
    }
    private sealed class TransactionScheduler(AppDbContext db, bool fail) : IOrderExpirationScheduler
    {
        public OrderExpirationMessage? Scheduled { get; private set; }
        public async Task ScheduleAsync(OrderExpirationMessage message, DateTimeOffset dueAt, CancellationToken cancellationToken = default)
        {
            Assert.NotNull(db.Database.CurrentTransaction);
            Assert.Equal(message.OrderId, (await db.Orders.SingleAsync()).Id);
            Scheduled = message;
            if (fail) throw new IOException("Storage unavailable");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Commit_requires_durable_send_and_failure_rolls_back_order_and_voucher(bool queueFails)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new TestDb(options);
        await db.Database.EnsureCreatedAsync();
        var user = new User { Id = 1, UserName = "schedule@test.com", Email = "schedule@test.com" };
        var product = new Product { Id = 1, Title = "Plan", Slug = "plan", Price = 100, AccessDurationMonths = 1, IsActive = true };
        var voucher = new Voucher { Id = 1, Code = "SCHEDULE", Value = 10 };
        db.AddRange(user, product, voucher);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var scheduler = new TransactionScheduler(db, queueFails);
        var handler = new OrderHandler(db, new FakePaymentHandler(), new(db),
            Options.Create(new OrderExpirationOptions()), TimeProvider.System,
            TestBusinessTime.Create(TimeProvider.System), expirationScheduler: scheduler);
        var result = await handler.CreateAsync(new CreateOrderRequest
            { UserId = user.Email!, ProductId = product.Id, VoucherId = voucher.Id });
        Assert.NotNull(scheduler.Scheduled);
        Assert.Equal(!queueFails, result.IsSuccess);
        db.ChangeTracker.Clear();
        Assert.Equal(queueFails ? 0 : 1, await db.Orders.CountAsync());
        Assert.Equal(queueFails ? 0 : 1, await db.VoucherRedemptions.CountAsync());
    }
}