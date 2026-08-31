using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Dima.Tests.Orders;

public class RefundTests
{
    private readonly ITestOutputHelper _output;
    public RefundTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Refund_started_more_than_14_days_ago_is_rejected()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseInMemoryDatabase($"DimaTests-{Guid.NewGuid()}")
                    .Options;
        await using var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "test@test.com",
            Email = "test@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

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

        var now = DateTime.Now;

        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Product = product,

            OriginalPrice = 100m,
            DiscountAmount = 0m,
            Total = 100m,

            Status = EOrderStatus.Paid,

            ExternalReference = "pi_test_001",

            PaidAt = now.AddDays(-15),
            AccessStartsAt = now.AddDays(-15),
            AccessEndsAt = now.AddDays(15)
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var paymentHandler = new FakePaymentHandler();

        var handler = new OrderHandler(
            context,
            paymentHandler);

        var request = new RefundOrderRequest
        {
            Id = order.Id,
            UserId = user.Email!
        };

        var result = await handler.RefundAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains("[E214]", result.Message);

        Assert.False(paymentHandler.RefundWasCalled);
        _output.WriteLine($"Code: {result.Code}");
        _output.WriteLine($"Message: {result.Message}");
        _output.WriteLine($"Stripe called: {paymentHandler.RefundWasCalled}");
    }
    [Fact]
    public async Task Refund_future_plan_is_allowed_after_14_days()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"DimaTests-{Guid.NewGuid()}")
            .Options;

        await using var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "future@test.com",
            Email = "future@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Title = "Plano Mensal",
            Description = "Plano futuro de teste",
            Price = 100m,
            IsActive = true,
            AccessDurationMonths = 1
        };

        context.Products.Add(product);
        await context.SaveChangesAsync();

        var now = DateTime.Now;

        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Product = product,

            OriginalPrice = 100m,
            DiscountAmount = 0m,
            Total = 100m,

            Status = EOrderStatus.Paid,
            ExternalReference = "pi_test_future_001",

            PaidAt = now.AddDays(-30),

            // Ainda não começou
            AccessStartsAt = now.AddDays(10),
            AccessEndsAt = now.AddDays(40)
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var paymentHandler = new FakePaymentHandler();

        var handler = new OrderHandler(
            context,
            paymentHandler);

        var request = new RefundOrderRequest
        {
            Id = order.Id,
            UserId = user.Email!,
            RefundReason = ERefundReason.NotUsingProduct
        };

        var result = await handler.RefundAsync(request);

        _output.WriteLine($"Code: {result.Code}");
        _output.WriteLine($"Message: {result.Message}");
        _output.WriteLine($"Stripe called: {paymentHandler.RefundWasCalled}");
        _output.WriteLine($"Order status: {order.Status}");
        _output.WriteLine($"Refund reference: {order.RefundReference}");

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Code);

        Assert.True(paymentHandler.RefundWasCalled);

        Assert.Equal(
            EOrderStatus.RefundPending,
            order.Status);

        Assert.Equal(
            "re_test_refund",
            order.RefundReference);
    }
    [Fact]
    public async Task ConfirmRefund_succeeded_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"DimaTests-{Guid.NewGuid()}")
            .Options;

        await using var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "refund@test.com",
            Email = "refund@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

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

        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Product = product,

            OriginalPrice = 100m,
            DiscountAmount = 0m,
            Total = 100m,

            Status = EOrderStatus.RefundPending,

            ExternalReference = "pi_test_refund_001",
            RefundReference = "re_test_refund_001",

            PaidAt = DateTime.Now.AddDays(-2),
            AccessStartsAt = DateTime.Now.AddDays(-2),
            AccessEndsAt = DateTime.Now.AddDays(28)
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var paymentHandler = new FakePaymentHandler();

        var handler = new OrderHandler(
            context,
            paymentHandler);

        var firstResult = await handler.ConfirmRefundAsync(
            "pi_test_refund_001",
            "re_test_refund_001",
            "succeeded",
            null);

        var firstRefundedAt = order.RefundedAt;

        await Task.Delay(50);

        var secondResult = await handler.ConfirmRefundAsync(
            "pi_test_refund_001",
            "re_test_refund_001",
            "succeeded",
            null);

        _output.WriteLine($"First result: {firstResult.Message}");
        _output.WriteLine($"Second result: {secondResult.Message}");
        _output.WriteLine($"RefundedAt: {order.RefundedAt}");

        Assert.True(firstResult.IsSuccess);
        Assert.True(secondResult.IsSuccess);

        Assert.Equal(
            EOrderStatus.Refunded,
            order.Status);

        Assert.NotNull(firstRefundedAt);

        Assert.Equal(
            firstRefundedAt,
            order.RefundedAt);
    }
    [Fact]
    public async Task CreateOrder_is_blocked_when_future_refund_is_pending()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"DimaTests-{Guid.NewGuid()}")
            .Options;

        await using var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "scheduled@test.com",
            Email = "scheduled@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

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

        var now = DateTime.Now;

        var futureOrder = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Product = product,

            OriginalPrice = 100m,
            DiscountAmount = 0m,
            Total = 100m,

            Status = EOrderStatus.RefundPending,

            ExternalReference = "pi_test_pending_001",
            RefundReference = "re_test_pending_001",

            PaidAt = now.AddDays(-5),
            AccessStartsAt = now.AddDays(20),
            AccessEndsAt = now.AddDays(50)
        };

        context.Orders.Add(futureOrder);
        await context.SaveChangesAsync();

        var paymentHandler = new FakePaymentHandler();

        var handler = new OrderHandler(
            context,
            paymentHandler);

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id
        };

        var result = await handler.CreateAsync(request);

        _output.WriteLine($"Code: {result.Code}");
        _output.WriteLine($"Message: {result.Message}");
        _output.WriteLine($"Existing status: {futureOrder.Status}");

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains("[E176]", result.Message);

        Assert.Single(context.Orders);
    }
    [Fact]
    public async Task Refund_uses_stable_idempotency_key()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"DimaTests-{Guid.NewGuid()}")
            .Options;

        await using var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "idempotency@test.com",
            Email = "idempotency@test.com"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

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

        var now = DateTime.Now;

        var order = new Order
        {
            UserId = user.Id,
            ProductId = product.Id,
            Product = product,

            OriginalPrice = 100m,
            DiscountAmount = 0m,
            Total = 100m,

            Status = EOrderStatus.Paid,
            ExternalReference = "pi_test_idempotency_001",

            PaidAt = now.AddDays(-2),
            AccessStartsAt = now.AddDays(-2),
            AccessEndsAt = now.AddDays(28)
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        var paymentHandler = new FakePaymentHandler();

        var handler = new OrderHandler(
            context,
            paymentHandler);

        var request = new RefundOrderRequest
        {
            Id = order.Id,
            UserId = user.Email!,
            RefundReason = ERefundReason.NotUsingProduct
        };

        var result = await handler.RefundAsync(request);

        var expectedKey = $"refund-order-{order.Id}";

        _output.WriteLine($"Code: {result.Code}");
        _output.WriteLine($"Stripe called: {paymentHandler.RefundWasCalled}");
        _output.WriteLine($"External reference: {paymentHandler.LastExternalReference}");
        _output.WriteLine($"Idempotency key: {paymentHandler.LastIdempotencyKey}");

        Assert.True(result.IsSuccess);

        Assert.True(paymentHandler.RefundWasCalled);

        Assert.Equal(
            order.ExternalReference,
            paymentHandler.LastExternalReference);

        Assert.Equal(
            expectedKey,
            paymentHandler.LastIdempotencyKey);
    }
}