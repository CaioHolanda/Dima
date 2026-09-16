using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Admin;

public class AdminOrderDetailsTests
{
    [Fact]
    public async Task Details_and_list_preserve_purchase_after_product_and_voucher_changes()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var product = new Product { Title = "Plano", Slug = "plano", Description = "Plano", Price = 100, AccessDurationMonths = 1 };
        var voucher = new Voucher { Code = "COMPRA10", Title = "Voucher", Description = "Voucher", Value = 10, DiscountType = EVoucherDiscountType.FixedAmount };
        var user = new User { Email = "cliente@test.com", UserName = "cliente" };
        context.AddRange(product, voucher, user);
        await context.SaveChangesAsync();
        var order = new Order
        {
            ProductId = product.Id, UserId = user.Id, VoucherId = voucher.Id,
            VoucherCodeSnapshot = voucher.Code, VoucherValueSnapshot = voucher.Value,
            VoucherDiscountTypeSnapshot = voucher.DiscountType,
            OriginalPrice = 100, DiscountAmount = 10, Total = 90, AccessDurationMonths = 1,
            Gateway = EPaymentGateway.Stripe, ExternalReference = "pi_compra", PaymentSessionId = "cs_compra",
            PaymentSessionExpiresAt = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero),
            ExpiresAt = new DateTimeOffset(2026, 9, 17, 12, 0, 0, TimeSpan.Zero),
            Status = EOrderStatus.RefundPending, PaidAt = new DateTime(2026, 9, 16, 11, 0, 0),
            RefundReference = "re_compra", RefundFailureReason = "Falha no provedor",
            RefundReason = ERefundReason.TechnicalIssue, RefundReasonDetails = "Comentário"
        };
        context.Add(order);
        await context.SaveChangesAsync();
        product.Price = 120;
        voucher.Code = "ALTERADO";
        voucher.Value = 50;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var handler = new AdminOrderHandler(context);
        var response = await handler.GetByIdAsync(order.Id);
        Assert.True(response.IsSuccess);
        var detail = Assert.IsType<AdminOrderDetails>(response.Data);
        Assert.Equal(100, detail.OriginalPrice);
        Assert.Equal(10, detail.DiscountAmount);
        Assert.Equal(90, detail.Total);
        Assert.Equal("COMPRA10", detail.VoucherCode);
        Assert.Equal(10, detail.VoucherValueSnapshot);
        Assert.Equal(EVoucherDiscountType.FixedAmount, detail.VoucherDiscountTypeSnapshot);
        Assert.Equal("cliente@test.com", detail.UserEmail);
        Assert.Equal(EPaymentGateway.Stripe, detail.Gateway);
        Assert.Equal("pi_compra", detail.ExternalReference);
        Assert.Equal("cs_compra", detail.PaymentSessionId);
        Assert.Equal(order.PaymentSessionExpiresAt, detail.PaymentSessionExpiresAt);
        Assert.Equal(order.ExpiresAt, detail.ExpiresAt);
        Assert.Equal(order.PaidAt, detail.PaidAt);
        Assert.Equal(EOrderStatus.RefundPending, detail.Status);
        Assert.Equal("re_compra", detail.RefundReference);
        Assert.Equal("Falha no provedor", detail.RefundFailureReason);
        Assert.Equal(ERefundReason.TechnicalIssue, detail.RefundReason);
        Assert.Equal("Comentário", detail.RefundReasonDetails);
        var list = await handler.GetAllAsync(new GetAllAdminOrdersRequest());
        Assert.Equal("COMPRA10", Assert.Single(list.Data!).VoucherCode);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(null, EOrderStatus.WaintingPayment)]
    [InlineData(EPaymentGateway.NotApplicable, EOrderStatus.Paid)]
    [InlineData(EPaymentGateway.Stripe, EOrderStatus.Expired)]
    [InlineData(EPaymentGateway.Stripe, EOrderStatus.Refunded)]
    public async Task Preserves_nullable_gateway_free_orders_expiration_and_refund(EPaymentGateway? gateway, EOrderStatus status)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var product = new Product { Title = "Plano", Slug = "plano", Description = "Plano" };
        var voucher = new Voucher { Code = "ATUAL", Title = "Voucher", Description = "Voucher" };
        var user = new User { Email = "cliente@test.com", UserName = "cliente" };
        context.AddRange(product, voucher, user);
        await context.SaveChangesAsync();
        var expired = status == EOrderStatus.Expired ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        var refunded = status == EOrderStatus.Refunded ? DateTime.Now : (DateTime?)null;
        var order = new Order { ProductId = product.Id, UserId = user.Id, VoucherId = voucher.Id,
            Gateway = gateway, Status = status, ExpiredAt = expired, RefundedAt = refunded,
            OriginalPrice = 100, DiscountAmount = 100, Total = 0 };
        context.Add(order);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var handler = new AdminOrderHandler(context);
        var response = await handler.GetByIdAsync(order.Id);
        Assert.True(response.IsSuccess);
        Assert.Equal(gateway, response.Data!.Gateway);
        Assert.Equal(status, response.Data.Status);
        Assert.Equal(expired, response.Data.ExpiredAt);
        Assert.Equal(refunded, response.Data.RefundedAt);
        Assert.Null(response.Data.VoucherCode);
        Assert.Null(response.Data.ExternalReference);
        Assert.Null(response.Data.PaymentSessionId);
        Assert.Null(response.Data.PaymentSessionExpiresAt);
        Assert.Null(Assert.Single((await handler.GetAllAsync(new GetAllAdminOrdersRequest())).Data!).VoucherCode);
        Assert.Empty(context.ChangeTracker.Entries());
        Assert.Equal(404, (await handler.GetByIdAsync(long.MaxValue)).Code);
    }

    private static AppDbContext CreateContext(SqliteConnection connection)
        => new TestContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
    private sealed class TestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToList()) entity.RemoveCheckConstraint(constraint.Name!);
            modelBuilder.Entity<Order>().Property(x => x.RowVersion).HasDefaultValue(new byte[8]);
        }
    }
}