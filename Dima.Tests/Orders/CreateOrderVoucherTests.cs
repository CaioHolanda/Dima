using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Api.Services;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;
using Dima.Api.Configuration;
using Microsoft.Extensions.Options;
using Dima.Api.Common.Api;
using Dima.Core.Responses;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dima.Tests.Orders;

public class CreateOrderVoucherTests
{
    private sealed class FixedTimeProvider(
    DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
    private sealed class UnexpectedSessionCloser
    : IPaymentSessionCloser
    {
        public int Calls { get; private set; }

        public Task<Response<bool>> CloseAsync(string sessionId)
        {
            Calls++;

            throw new InvalidOperationException(
                "Este pedido não possui sessão de pagamento.");
        }
    }
    private sealed class ControlledSessionCloser(
    Response<bool> result) : IPaymentSessionCloser
    {
        public int Calls { get; private set; }
        public string? LastSessionId { get; private set; }

        public Task<Response<bool>> CloseAsync(string sessionId)
        {
            Calls++;
            LastSessionId = sessionId;

            return Task.FromResult(result);
        }
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
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            TimeProvider.System);

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

        Assert.Equal(voucher.Code,result.Data.VoucherCodeSnapshot);
        Assert.Equal((EVoucherDiscountType?)voucher.DiscountType,
                        result.Data.VoucherDiscountTypeSnapshot);

        Assert.Equal(
            (decimal?)voucher.Value,
            result.Data.VoucherValueSnapshot);

        var storedOrder =
            await context.Orders.SingleAsync();

        Assert.Equal(voucher.Code,storedOrder.VoucherCodeSnapshot);
        Assert.Equal((EVoucherDiscountType?)voucher.DiscountType,
            storedOrder.VoucherDiscountTypeSnapshot);

        Assert.Equal(
            (decimal?)voucher.Value,
            storedOrder.VoucherValueSnapshot);
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
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            TimeProvider.System);

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
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            TimeProvider.System);

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
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            TimeProvider.System);

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
        Assert.Null(storedOrder.ExpiresAt);
        Assert.Null(storedOrder.ExpiredAt);
        Assert.Null(storedOrder.PaymentSessionId);
        Assert.Null(storedOrder.PaymentSessionExpiresAt);
    }

    [Fact]
    public async Task
    CreateOrder_rejects_expired_voucher_when_called_directly()
    {
        await using var context =
            await CreateContextAsync(
                productPrice: 100m,
                voucherType:
                    EVoucherDiscountType.FixedAmount,
                voucherValue: 25m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.EndsAt = DateTime.Now.AddDays(-1);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            TimeProvider.System);

        var request = new CreateOrderRequest
        {
            UserId = user.Email!,
            ProductId = product.Id,
            VoucherId = voucher.Id
        };

        var result =
            await handler.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.Code);
        Assert.Contains("[E242]", result.Message);

        Assert.Empty(context.Orders);
        Assert.Empty(context.VoucherRedemptions);
    }

    [Fact]
    public async Task ExpireOrder_without_session_releases_voucher_once()
    {
        var nowUtc = new DateTimeOffset(
            2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        await using var context = await CreateContextAsync(
            productPrice: 100m,
            voucherType: EVoucherDiscountType.FixedAmount,
            voucherValue: 25m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        var deadline = nowUtc.AddMinutes(-1);

        var order = new Order
        {
            UserId = user.Id,

            ProductId = product.Id,
            Product = product,

            VoucherId = voucher.Id,
            Voucher = voucher,

            VoucherCodeSnapshot = voucher.Code,
            VoucherDiscountTypeSnapshot = voucher.DiscountType,
            VoucherValueSnapshot = voucher.Value,

            OriginalPrice = 100m,
            DiscountAmount = 25m,
            Total = 75m,

            AccessDurationMonths = product.AccessDurationMonths,

            Status = EOrderStatus.WaintingPayment,
            Gateway = EPaymentGateway.Stripe,

            CreatedAt = nowUtc.AddMinutes(-31).LocalDateTime,
            UpdatedAt = nowUtc.AddMinutes(-31).LocalDateTime,

            ExpiresAt = deadline
        };

        var redemption = new VoucherRedemption
        {
            Order = order,
            Voucher = voucher,
            VoucherId = voucher.Id,
            UserId = user.Id,

            Status = EVoucherRedemptionStatus.Reserved,
            ReservedAt = order.CreatedAt
        };

        context.Orders.Add(order);
        context.VoucherRedemptions.Add(redemption);

        await context.SaveChangesAsync();

        var orderId = order.Id;

        context.ChangeTracker.Clear();

        var sessionCloser = new UnexpectedSessionCloser();

        var service = new OrderExpirationService(
            context,
            sessionCloser,
            new FixedTimeProvider(nowUtc),
            NullLogger<OrderExpirationService>.Instance);

        var result = await service.ExpireAsync(orderId);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data);

        context.ChangeTracker.Clear();

        var storedOrder = await context.Orders
            .AsNoTracking()
            .SingleAsync();

        var storedRedemption = await context.VoucherRedemptions
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(EOrderStatus.Expired, storedOrder.Status);
        Assert.Equal((DateTimeOffset?)nowUtc, storedOrder.ExpiredAt);
        Assert.Equal((DateTimeOffset?)deadline, storedOrder.ExpiresAt);

        Assert.Equal(
            EVoucherRedemptionStatus.Released,
            storedRedemption.Status);

        Assert.Equal(
            (DateTime?)nowUtc.LocalDateTime,
            storedRedemption.ReleasedAt);

        Assert.Null(storedRedemption.RedeemedAt);
        Assert.Equal(0, sessionCloser.Calls);

        // Repete a operação cinco minutos depois.
        var repeatedService = new OrderExpirationService(
            context,
            sessionCloser,
            new FixedTimeProvider(nowUtc.AddMinutes(5)),
            NullLogger<OrderExpirationService>.Instance);

        var repeatedResult = await repeatedService.ExpireAsync(orderId);

        Assert.True(repeatedResult.IsSuccess);
        Assert.True(repeatedResult.Data);

        context.ChangeTracker.Clear();

        var repeatedOrder = await context.Orders
            .AsNoTracking()
            .SingleAsync();

        var repeatedRedemption = await context.VoucherRedemptions
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            (DateTimeOffset?)nowUtc,
            repeatedOrder.ExpiredAt);

        Assert.Equal(
            (DateTime?)nowUtc.LocalDateTime,
            repeatedRedemption.ReleasedAt);

        Assert.Equal(0, sessionCloser.Calls);

        Assert.True(
            (await context.Vouchers.AsNoTracking().SingleAsync())
            .IsActive);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    public async Task CreateOrder_stores_configured_expiration(
    int lifetimeMinutes)
    {
        await using var context = await CreateContextAsync(
            productPrice: 100m,
            voucherType: EVoucherDiscountType.FixedAmount,
            voucherValue: 25m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        context.ChangeTracker.Clear();

        var utcNow = new DateTimeOffset(
            2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

        var handler = new OrderHandler(
            context,
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions
            {
                PendingOrderLifetimeMinutes = lifetimeMinutes
            }),
            new FixedTimeProvider(utcNow));

        var result = await handler.CreateAsync(
            new CreateOrderRequest
            {
                UserId = user.Email!,
                ProductId = product.Id,
                VoucherId = voucher.Id
            });

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);

        context.ChangeTracker.Clear();

        var storedOrder = await context.Orders
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            EOrderStatus.WaintingPayment,
            storedOrder.Status);

        Assert.NotNull(storedOrder.ExpiresAt);

        Assert.Equal(
            utcNow.AddMinutes(lifetimeMinutes),
            storedOrder.ExpiresAt.Value);

        Assert.Equal(
            TimeSpan.Zero,
            storedOrder.ExpiresAt.Value.Offset);

        Assert.Null(storedOrder.ExpiredAt);
        Assert.Null(storedOrder.PaymentSessionId);
        Assert.Null(storedOrder.PaymentSessionExpiresAt);
    }

    [Theory]
    [InlineData(true, 409)]
    [InlineData(true, 502)]
    [InlineData(false, 409)]
    public async Task ExpireOrder_preserves_reservation_when_payment_is_uncertain(
    bool hasSessionId,
    int failureCode)
    {
        var createdAtUtc = new DateTimeOffset(
            2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

        await using var context = await CreateContextAsync(
            productPrice: 100m,
            voucherType: EVoucherDiscountType.FixedAmount,
            voucherValue: 25m);

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();
        context.ChangeTracker.Clear();
        var orderHandler = new OrderHandler(
            context,
            new FakePaymentHandler(),
            new VoucherEligibilityService(context),
            Options.Create(new OrderExpirationOptions()),
            new FixedTimeProvider(createdAtUtc));

        var creationResult = await orderHandler.CreateAsync(
            new CreateOrderRequest
            {
                UserId = user.Email!,
                ProductId = product.Id,
                VoucherId = voucher.Id
            });

        Assert.True(
            creationResult.IsSuccess,
            $"Falha ao preparar o pedido: " +
            $"{creationResult.Code} - {creationResult.Message}");
        
        Assert.NotNull(creationResult.Data);

        context.ChangeTracker.Clear();

        var order = await context.Orders.SingleAsync();

        var deadline = createdAtUtc.AddMinutes(60);

        order.ExpiresAt = deadline;
        order.PaymentSessionExpiresAt = deadline;

        order.PaymentSessionId = hasSessionId
            ? "cs_test_uncertain_payment"
            : null;

        await context.SaveChangesAsync();

        var orderId = order.Id;

        context.ChangeTracker.Clear();

        var sessionCloser = new ControlledSessionCloser(
            new Response<bool>(
                false,
                failureCode,
                "Não foi possível confirmar o encerramento."));

        var service = new OrderExpirationService(
            context,
            sessionCloser,
            new FixedTimeProvider(deadline.AddMinutes(1)),
            NullLogger<OrderExpirationService>.Instance);

        var result = await service.ExpireAsync(orderId);

        Assert.False(result.IsSuccess);
        Assert.False(result.Data);
        Assert.Equal(failureCode, result.Code);

        context.ChangeTracker.Clear();

        var storedOrder = await context.Orders
            .AsNoTracking()
            .SingleAsync();

        var storedRedemption = await context.VoucherRedemptions
            .AsNoTracking()
            .SingleAsync();

        Assert.Equal(
            EOrderStatus.WaintingPayment,
            storedOrder.Status);

        Assert.Null(storedOrder.ExpiredAt);
        Assert.Equal((DateTimeOffset?)deadline, storedOrder.ExpiresAt);

        Assert.Equal(
            EVoucherRedemptionStatus.Reserved,
            storedRedemption.Status);

        Assert.Null(storedRedemption.ReleasedAt);
        Assert.Null(storedRedemption.RedeemedAt);

        Assert.Equal(hasSessionId ? 1 : 0, sessionCloser.Calls);

        if (hasSessionId)
        {
            Assert.Equal(
                "cs_test_uncertain_payment",
                sessionCloser.LastSessionId);
        }
        else
        {
            Assert.Null(sessionCloser.LastSessionId);
            Assert.Contains("[E268]", result.Message);
        }
    }
}