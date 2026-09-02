using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Orders;

public class OrderAccessDurationTests
{
    [Fact]
    public async Task ConfirmPayment_uses_order_duration_snapshot_when_product_changes()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        await using var context =
            new AppDbContext(options);

        var user = new User
        {
            UserName = "duration@test.com",
            Email = "duration@test.com"
        };

        var product = new Product
        {
            Title = "Plano Semestral",
            Description = "Plano para teste",
            Price = 659.99m,
            Slug = "plano-semestral",
            IsActive = true,
            AccessDurationMonths = 6
        };

        context.Users.Add(user);
        context.Products.Add(product);

        await context.SaveChangesAsync();

        var order = new Order
        {
            ProductId = product.Id,
            UserId = user.Id,
            OriginalPrice = 659.99m,
            DiscountAmount = 0m,
            Total = 659.99m,
            AccessDurationMonths = 6,
            Gateway = EPaymentGateway.Stripe,
            Status = EOrderStatus.WaintingPayment
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        product.AccessDurationMonths = 12;
        await context.SaveChangesAsync();

        var orderNumber = order.Number;
        var userId = user.Id;

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler());

        var beforeConfirmation = DateTime.Now;

        var result = await handler.ConfirmPaymentAsync(
            orderNumber,
            "pi_test_duration",
            65999,
            "brl",
            userId.ToString());

        var afterConfirmation = DateTime.Now;

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Code);
        Assert.NotNull(result.Data);

        Assert.Equal(
            EOrderStatus.Paid,
            result.Data.Status);

        Assert.NotNull(result.Data.AccessStartsAt);
        Assert.NotNull(result.Data.AccessEndsAt);

        Assert.InRange(
            result.Data.AccessStartsAt.Value,
            beforeConfirmation,
            afterConfirmation);

        Assert.Equal(
            result.Data.AccessStartsAt.Value.AddMonths(6),
            result.Data.AccessEndsAt.Value);

        var storedOrder =
            await context.Orders
                .AsNoTracking()
                .SingleAsync();

        Assert.NotNull(storedOrder.AccessEndsAt);

        Assert.Equal(
            storedOrder.AccessStartsAt!.Value.AddMonths(6),
            storedOrder.AccessEndsAt.Value);
    }
}