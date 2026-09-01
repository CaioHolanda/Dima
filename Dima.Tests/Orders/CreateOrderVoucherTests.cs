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

        var storedVoucher =
            await context.Vouchers.SingleAsync();

        Assert.True(storedVoucher.IsActive);
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