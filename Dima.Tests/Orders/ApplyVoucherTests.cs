using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Orders;

public class ApplyVoucherTests
{
    [Fact]
    public async Task ApplyVoucher_normalizes_code_and_returns_quote()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var product = new Product
        {
            Title = "Plano Mensal",
            Description = "Plano de teste",
            Price = 100m,
            IsActive = true,
            AccessDurationMonths = 1
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var voucher = new Voucher
        {
            Code = "SAVE25",
            Title = "Desconto de teste",
            Description = "Voucher de teste",
            DiscountType =
                EVoucherDiscountType.FixedAmount,
            Value = 25m,
            ProductId = product.Id,
            IsActive = true
        };

        context.Vouchers.Add(voucher);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new VoucherHandler(context);

        var request = new ApplyVoucherRequest
        {
            Code = "  save25  ",
            ProductId = product.Id
        };

        var result =
            await handler.ApplyAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Code);
        Assert.NotNull(result.Data);

        Assert.Equal(voucher.Id, result.Data.VoucherId);
        Assert.Equal("SAVE25", result.Data.Code);
        Assert.Equal(25m, result.Data.DiscountAmount);
        Assert.Equal(75m, result.Data.Total);
    }
}