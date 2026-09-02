using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Orders;

public class CreateOrderVoucherTests
{
    [Fact]
    public async Task CreateOrder_applies_fixed_voucher_and_keeps_it_active()
    {
        await using var context =
            await CreateContextAsync(
                productPrice: 100m,
                voucherType: EVoucherDiscountType.FixedAmount,
                voucherValue: 25m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler());

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id,
            VoucherId = voucher.Id
        };

        var result = await handler.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.Code);
        Assert.NotNull(result.Data);

        Assert.Equal(100m, result.Data.OriginalPrice);
        Assert.Equal(25m, result.Data.DiscountAmount);
        Assert.Equal(75m, result.Data.Total);

        var storedOrder =
            await context.Orders.SingleAsync();

        Assert.Equal(25m, storedOrder.DiscountAmount);
        Assert.Equal(75m, storedOrder.Total);
        Assert.Equal(voucher.Id, storedOrder.VoucherId);

        Assert.Equal(product.AccessDurationMonths,result.Data.AccessDurationMonths);

        var storedVoucher =
            await context.Vouchers.SingleAsync();

        Assert.True(storedVoucher.IsActive);
        Assert.Equal(
            EOrderStatus.WaintingPayment,
            result.Data.Status);

        Assert.Equal(
            EPaymentGateway.Stripe,
            result.Data.Gateway);
    }

    [Fact]
    public async Task CreateOrder_applies_and_rounds_percentage_voucher()
    {
        await using var context =
            await CreateContextAsync(
                productPrice: 10.05m,
                voucherType: EVoucherDiscountType.Percentage,
                voucherValue: 10m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler());

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id,
            VoucherId = voucher.Id
        };

        var result = await handler.CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        Assert.Equal(10.05m, result.Data.OriginalPrice);
        Assert.Equal(1.01m, result.Data.DiscountAmount);
        Assert.Equal(9.04m, result.Data.Total);
    }

    [Fact]
    public async Task CreateOrder_rejects_fixed_voucher_greater_than_product()
    {
        await using var context =
            await CreateContextAsync(
                productPrice: 100m,
                voucherType: EVoucherDiscountType.FixedAmount,
                voucherValue: 150m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler());

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id,
            VoucherId = voucher.Id
        };

        var result = await handler.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains("[E229]", result.Message);

        Assert.Empty(context.Orders);

        var storedVoucher =
            await context.Vouchers.SingleAsync();

        Assert.True(storedVoucher.IsActive);
    }
    [Fact]
    public async Task CreateOrder_completes_free_order_internally()
    {
        await using var context =
            await CreateContextAsync(
                productPrice: 100m,
                voucherType:
                    EVoucherDiscountType.Percentage,
                voucherValue: 100m);

        var user = await context.Users.SingleAsync();
        var product =
            await context.Products.SingleAsync();
        var voucher =
            await context.Vouchers.SingleAsync();

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler());

        var beforeCreation = DateTime.Now;

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id,
            VoucherId = voucher.Id
        };

        var result =
            await handler.CreateAsync(request);

        var afterCreation = DateTime.Now;

        Assert.True(result.IsSuccess);
        Assert.Equal(201, result.Code);
        Assert.NotNull(result.Data);

        Assert.Equal(100m, result.Data.OriginalPrice);
        Assert.Equal(100m, result.Data.DiscountAmount);
        Assert.Equal(0m, result.Data.Total);

        Assert.Equal(EOrderStatus.Paid,result.Data.Status);

        Assert.Equal(EPaymentGateway.NotApplicable,result.Data.Gateway);

        Assert.Null(result.Data.ExternalReference);
        Assert.NotNull(result.Data.PaidAt);
        Assert.NotNull(result.Data.AccessStartsAt);
        Assert.NotNull(result.Data.AccessEndsAt);

        Assert.InRange(
            result.Data.PaidAt.Value,
            beforeCreation,
            afterCreation);

        Assert.Equal(
            result.Data.AccessStartsAt.Value.AddMonths(
            result.Data.AccessDurationMonths),
            result.Data.AccessEndsAt.Value);

        var storedOrder =
            await context.Orders.SingleAsync();
        Assert.Equal(
            product.AccessDurationMonths,
            storedOrder.AccessDurationMonths);

        Assert.Equal(EOrderStatus.Paid, storedOrder.Status);
        Assert.Equal(
            EPaymentGateway.NotApplicable,
            storedOrder.Gateway);

        Assert.Equal(0m, storedOrder.Total);
        Assert.Equal(product.AccessDurationMonths,result.Data.AccessDurationMonths);
        Assert.Null(storedOrder.ExternalReference);

        var storedVoucher =
            await context.Vouchers.SingleAsync();

        Assert.True(storedVoucher.IsActive);
    }

    private static async Task<AppDbContext> CreateContextAsync(
        decimal productPrice,
        EVoucherDiscountType voucherType,
        decimal voucherValue)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "voucher@test.com",
            Email = "voucher@test.com"
        };

        var product = new Product
        {
            Title = "Plano de teste",
            Description = "Produto para teste de voucher",
            Price = productPrice,
            IsActive = true,
            AccessDurationMonths = 1
        };

        context.Users.Add(user);
        context.Products.Add(product);

        await context.SaveChangesAsync();

        var voucher = new Voucher
        {
            Code = $"TEST{Guid.NewGuid():N}"[..12],
            Title = "Voucher de teste",
            Description = "Voucher para teste do pedido",
            DiscountType = voucherType,
            Value = voucherValue,
            IsActive = true
        };

        context.Vouchers.Add(voucher);

        await context.SaveChangesAsync();

        return context;
    }
}