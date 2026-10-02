using System.Text.Json;
using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Core.Common.Time;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Transactions;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Requests.Reports;
using Dima.Core.Requests.Users;
using Dima.Tests.Orders.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Time;

public class UtcTimeTests
{
    private static readonly DateTimeOffset Instant = new(2026, 9, 17, 0, 30, 0, TimeSpan.Zero);
    private static TimeProvider Clock(string zone = "Pacific/Auckland") =>
        new FixedClock(Instant, TimeZoneInfo.FindSystemTimeZoneById(zone));
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static OrderHandler Orders(AppDbContext db, TimeProvider clock, FakePaymentHandler? payment = null) =>
        new(db, payment ?? new(), new(db), Options.Create(new OrderExpirationOptions()), clock, TestBusinessTime.Create(clock), expirationScheduler: new RecordingExpirationScheduler());

    private static async Task<(User user, Product product)> Seed(AppDbContext db)
    {
        var user = new User { Email = "utc@test.com", UserName = "utc@test.com" };
        var product = new Product { Title = "UTC", Slug = "utc", Price = 100m, AccessDurationMonths = 1, IsActive = true };
        db.AddRange(user, product);
        await db.SaveChangesAsync();
        return (user, product);
    }

    [Fact]
    public async Task Creation_and_cancellation_persist_one_utc_instant_with_a_non_utc_host_clock()
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        var voucher = new Voucher { Code = "TODAY", Value = 10m, StartsAt = Instant.Date, EndsAt = Instant.Date };
        db.Add(voucher);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var handler = Orders(db, Clock());
        var created = await handler.CreateAsync(new CreateOrderRequest { UserId = user.Email!, ProductId = product.Id, VoucherId = voucher.Id });
        Assert.True(created.IsSuccess, created.Message);
        Assert.Equal(Instant.UtcDateTime, created.Data!.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, created.Data.CreatedAt.Kind);
        Assert.Equal(Instant.UtcDateTime, created.Data.UpdatedAt);
        Assert.Equal(Instant.AddMinutes(new OrderExpirationOptions().PendingOrderLifetimeMinutes), created.Data.ExpiresAt);
        var reservation = await db.VoucherRedemptions.SingleAsync();
        Assert.Equal(Instant.UtcDateTime, reservation.ReservedAt);
        var canceled = await handler.CancelAsync(new CancelOrderRequest { UserId = user.Email!, Id = created.Data.Id });
        Assert.True(canceled.IsSuccess, canceled.Message);
        Assert.Equal(Instant.UtcDateTime, canceled.Data!.UpdatedAt);
        Assert.Equal(Instant.UtcDateTime, reservation.ReleasedAt);
    }

    [Fact]
    public async Task Payment_uses_fixed_utc_clock_and_extends_existing_access()
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        var previous = new Order { ProductId = product.Id, UserId = user.Id, Status = EOrderStatus.Paid,
            Total = 100m, AccessStartsAt = Instant.UtcDateTime.AddDays(-10), AccessEndsAt = Instant.UtcDateTime.AddDays(20) };
        var pending = new Order { ProductId = product.Id, UserId = user.Id, Total = 100m, AccessDurationMonths = 1 };
        db.AddRange(previous, pending);
        await db.SaveChangesAsync();
        var result = await Orders(db, Clock("America/Sao_Paulo")).ConfirmPaymentAsync(pending.Number, "pi_utc", 10000, "brl", user.Id.ToString());
        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(Instant.UtcDateTime, result.Data!.PaidAt);
        Assert.Equal(DateTimeKind.Utc, result.Data.PaidAt!.Value.Kind);
        Assert.Equal(previous.AccessEndsAt, result.Data.AccessStartsAt);
        Assert.Equal(previous.AccessEndsAt!.Value.AddMonths(1), result.Data.AccessEndsAt);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task Refund_api_and_ui_time_rules_agree_at_the_14_day_boundary(int ticks, bool allowed)
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        var paidAt = Instant.UtcDateTime.AddDays(-14).AddTicks(-ticks);
        var order = new Order { ProductId = product.Id, UserId = user.Id, Status = EOrderStatus.Paid,
            Total = 100m, Gateway = EPaymentGateway.Stripe, ExternalReference = "pi_boundary",
            PaidAt = paidAt, AccessStartsAt = paidAt, AccessEndsAt = paidAt.AddMonths(1) };
        db.Add(order);
        await db.SaveChangesAsync();
        var payment = new FakePaymentHandler();
        var result = await Orders(db, Clock(), payment).RefundAsync(new RefundOrderRequest
            { Id = order.Id, UserId = user.Email!, RefundReason = ERefundReason.Other, RefundReasonDetails = "Teste UTC" });
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed, payment.RefundWasCalled);
        Assert.Equal(allowed, RefundTimeRules.IsWithinWindow(paidAt, Instant.UtcDateTime));
        if (allowed) Assert.Equal(Instant.UtcDateTime, result.Data!.UpdatedAt);
        else Assert.Contains("[E214]", result.Message);
    }

    [Theory]
    [InlineData("UTC", 17, true)]
    [InlineData("America/Sao_Paulo", 16, false)]
    [InlineData("Pacific/Auckland", 17, true)]
    public async Task Voucher_preview_and_order_creation_use_the_same_business_day(string zone, int day, bool allowed)
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        var voucher = new Voucher { Code = "DAY", Value = 10m, StartsAt = new DateTime(2026, 9, 17), EndsAt = new DateTime(2026, 9, 17) };
        db.Add(voucher);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var clock = Clock();
        var business = TestBusinessTime.Create(clock, zone);
        Assert.Equal(day, business.Now.Day);
        Assert.Equal(DateTimeKind.Unspecified, business.Now.Kind);
        var preview = await new VoucherHandler(db, new(db), business).ApplyAsync(new ApplyVoucherRequest
            { UserId = user.Email!, ProductId = product.Id, Code = voucher.Code });
        var order = await new OrderHandler(db, new FakePaymentHandler(), new(db), Options.Create(new OrderExpirationOptions()), clock, business, expirationScheduler: new Dima.Tests.Orders.Fakes.RecordingExpirationScheduler())
            .CreateAsync(new CreateOrderRequest { UserId = user.Email!, ProductId = product.Id, VoucherId = voucher.Id });
        Assert.Equal(allowed, preview.IsSuccess);
        Assert.Equal(allowed, order.IsSuccess);
        if (!allowed)
        {
            Assert.Contains("[E241]", preview.Message);
            Assert.Contains("[E241]", order.Message);
            Assert.Empty(await db.VoucherRedemptions.ToListAsync());
        }
    }

    [Fact]
    public async Task Database_roundtrip_tags_legacy_instant_as_utc_and_preserves_financial_civil_date()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        // Existing SQL Server CHECK constraints use LEN.
        connection.CreateFunction<string, int>("LEN", value => value.Length);
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var category = new Category { Title = "Calendar", UserId = "utc@test.com" };
        db.Add(category);
        await db.SaveChangesAsync();
        // Simulates a pre-DT18 row: identical clock value, without DateTime.Kind.
        var legacy = new DateTime(2026, 9, 17, 0, 30, 0, DateTimeKind.Unspecified);
        var civil = new DateTime(2026, 9, 16);
        var transaction = new Transaction { Title = "UTC", CategoryId = category.Id, UserId = category.UserId,
            CreatedAt = legacy, PaidOrReceivedAt = civil };
        db.Add(transaction);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var loaded = await db.Transactions.AsNoTracking().SingleAsync();
        Assert.Equal(legacy.Ticks, loaded.CreatedAt.Ticks);
        Assert.Equal(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        Assert.Equal(civil, loaded.PaidOrReceivedAt);
        Assert.Equal(DateTimeKind.Unspecified, loaded.PaidOrReceivedAt!.Value.Kind);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(loaded));
        Assert.EndsWith("Z", json.RootElement.GetProperty("CreatedAt").GetString());
        Assert.DoesNotContain("Z", json.RootElement.GetProperty("PaidOrReceivedAt").GetString());
        var browserValue = JsonSerializer.Deserialize<Transaction>(json.RootElement.GetRawText())!;
        Assert.Equal(DateTimeKind.Utc, browserValue.CreatedAt.Kind);
        Assert.Equal("16/09/2026 21:30", UtcInstant.FormatLocal(browserValue.CreatedAt, "dd/MM/yyyy HH:mm",
            TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")));
        Assert.Equal(Instant.UtcDateTime, browserValue.CreatedAt);
    }

    [Fact]
    public async Task Transaction_creation_uses_utc_without_converting_user_entered_civil_date()
    {
        await using var db = Database();
        var civil = new DateTime(2026, 9, 16);
        var clock = Clock();
        var result = await new TransactionHandler(db, clock, TestBusinessTime.Create(clock)).CreateAsync(
            new CreateTransactionRequest { Title = "Civil", UserId = "utc@test.com", CategoryId = 1,
                Amount = 10m, PaidOrReceivedAt = civil });
        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(Instant.UtcDateTime, result.Data!.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, result.Data.CreatedAt.Kind);
        Assert.Equal(civil, result.Data.PaidOrReceivedAt);
    }

    [Fact]
    public async Task Financial_summary_and_default_period_use_utc_month_at_the_month_boundary()
    {
        await using var db = Database();
        var instant = new DateTimeOffset(2026, 10, 1, 0, 30, 0, TimeSpan.Zero);
        var clock = new FixedClock(instant, TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"));
        var category = new Category { Title = "Calendar", UserId = "utc@test.com" };
        db.Add(category);
        await db.SaveChangesAsync();
        foreach (var (civil, amount) in new[] { (new DateTime(2026, 9, 30), 999m), (new DateTime(2026, 10, 1), 10m) })
            db.Add(new Transaction { Title = "Calendar", UserId = category.UserId, CategoryId = category.Id,
                Type = ETransactionType.Deposit, PaidOrReceivedAt = civil, Amount = amount });
        await db.SaveChangesAsync();
        var business = TestBusinessTime.Create(clock);
        var report = await new ReportHandler(db, business).GetFinancialSummaryReportAsync(
            new GetFinancialSummaryRequest { UserId = category.UserId });
        Assert.True(report.IsSuccess, report.Message);
        Assert.Equal(10m, report.Data!.Incomes);
        var request = new GetTransactionsByPeriodRequest { UserId = category.UserId };
        var period = await new TransactionHandler(db, clock, business).GetByPeriodAsync(request);
        Assert.True(period.IsSuccess, period.Message);
        Assert.Equal(new DateTime(2026, 10, 1), request.StartDate);
        Assert.Equal(new DateTime(2026, 10, 31), request.EndDate);
        Assert.Equal(10m, Assert.Single(period.Data!).Amount);
    }

    [Fact]
    public async Task Admin_access_uses_the_injected_utc_instant_at_the_end_boundary()
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        db.Add(new Order { ProductId = product.Id, UserId = user.Id, Status = EOrderStatus.Paid,
            AccessStartsAt = Instant.UtcDateTime.AddMonths(-1), AccessEndsAt = Instant.UtcDateTime });
        await db.SaveChangesAsync();
        var expired = await new AdminUserHandler(db, null!, Clock()).GetAllAsync(
            new GetAllAdminUsersRequest { IsPremium = true });
        Assert.True(expired.IsSuccess, expired.Message);
        Assert.Empty(expired.Data!);
        var before = new FixedClock(Instant.AddTicks(-1));
        var active = await new AdminUserHandler(db, null!, before).GetAllAsync(
            new GetAllAdminUsersRequest { IsPremium = true });
        Assert.True(active.IsSuccess, active.Message);
        Assert.Equal(user.Id, Assert.Single(active.Data!).Id);
    }

    [Fact]
    public async Task Refund_confirmation_records_utc_and_replay_preserves_the_original_instant()
    {
        await using var db = Database();
        var (user, product) = await Seed(db);
        var order = new Order { UserId = user.Id, ProductId = product.Id, Status = EOrderStatus.RefundPending,
            ExternalReference = "pi_utc", RefundReference = "re_utc", Total = 100m, Gateway = EPaymentGateway.Stripe };
        db.Add(order);
        await db.SaveChangesAsync();
        var confirmed = await Orders(db, Clock()).ConfirmRefundAsync("pi_utc", "re_utc", "succeeded", null);
        Assert.True(confirmed.IsSuccess, confirmed.Message);
        Assert.Equal(Instant.UtcDateTime, confirmed.Data!.RefundedAt);
        Assert.Equal(DateTimeKind.Utc, confirmed.Data.RefundedAt!.Value.Kind);
        var repeated = await Orders(db, new FixedClock(Instant.AddDays(1))).ConfirmRefundAsync("pi_utc", "re_utc", "succeeded", null);
        Assert.True(repeated.IsSuccess, repeated.Message);
        Assert.Equal(Instant.UtcDateTime, repeated.Data!.RefundedAt);
        Assert.Equal(Instant.UtcDateTime, repeated.Data.UpdatedAt);
    }

    [Fact]
    public void Every_legacy_instant_column_restores_utc_kind_without_shifting_ticks()
    {
        using var db = Database();
        var legacy = new DateTime(2026, 9, 17, 0, 30, 0);
        foreach (var (type, names) in new[] {
            (typeof(Order), new[] { "CreatedAt", "UpdatedAt", "PaidAt", "AccessStartsAt", "AccessEndsAt", "RefundedAt" }),
            (typeof(Transaction), new[] { "CreatedAt" }),
            (typeof(VoucherRedemption), new[] { "ReservedAt", "RedeemedAt", "ReleasedAt" }) })
        foreach (var name in names)
        {
            var converter = db.Model.FindEntityType(type)!.FindProperty(name)!.GetTypeMapping().Converter;
            Assert.NotNull(converter);
            var restored = Assert.IsType<DateTime>(converter.ConvertFromProvider(legacy));
            Assert.Equal(legacy.Ticks, restored.Ticks);
            Assert.Equal(DateTimeKind.Utc, restored.Kind);
        }
    }
}
