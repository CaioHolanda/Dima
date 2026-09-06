using Dima.Api.Data;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Microsoft.EntityFrameworkCore;

namespace Dima.Tests.Orders;

public class VoucherEligibilityTests
{
    [Fact]
    public async Task Evaluate_rejects_voucher_that_has_not_started()
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync(voucher =>
            {
                voucher.StartsAt =
                    now.AddDays(1);
            });

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E241]", result.Message);
    }

    [Fact]
    public async Task Evaluate_rejects_expired_voucher()
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync(voucher =>
            {
                voucher.EndsAt =
                    now.AddDays(-1);
            });

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E242]", result.Message);
    }

    [Fact]
    public async Task Evaluate_accepts_validity_boundaries_as_inclusive()
    {
        var now = new DateTime(
            2026,
            9,
            5,
            18,
            30,
            0);

        await using var context =
            await CreateContextAsync(voucher =>
            {
                voucher.StartsAt = now.Date;
                voucher.EndsAt = now.Date;
            });

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.True(result.IsEligible);
    }
    [Fact]
    public async Task Evaluate_accepts_voucher_assigned_to_current_user()
    {
        await using var context =
            await CreateContextAsync();

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.AssignedUserId = user.Id;
        await context.SaveChangesAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            DateTime.Now);

        Assert.True(result.IsEligible);
    }

    [Fact]
    public async Task Evaluate_rejects_voucher_assigned_to_another_user()
    {
        await using var context =
            await CreateContextAsync();

        var currentUser =
            await context.Users.SingleAsync();

        var anotherUser = new User
        {
            UserName = "another@test.com",
            Email = "another@test.com"
        };

        context.Users.Add(anotherUser);
        await context.SaveChangesAsync();

        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.AssignedUserId = anotherUser.Id;
        await context.SaveChangesAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            currentUser.Id,
            DateTime.Now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E243]", result.Message);
    }

    [Fact]
    public async Task Evaluate_rejects_voucher_for_another_product()
    {
        await using var context =
            await CreateContextAsync();

        var user = await context.Users.SingleAsync();
        var selectedProduct =
            await context.Products.SingleAsync();

        var anotherProduct = new Product
        {
            Title = "Outro plano",
            Description = "Produto incompatível",
            Price = 200m,
            IsActive = true,
            AccessDurationMonths = 2
        };

        context.Products.Add(anotherProduct);
        await context.SaveChangesAsync();

        var voucher = await context.Vouchers.SingleAsync();

        voucher.ProductId = anotherProduct.Id;
        await context.SaveChangesAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            selectedProduct,
            user.Id,
            DateTime.Now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E233]", result.Message);
    }
    [Theory]
    [InlineData(EVoucherRedemptionStatus.Reserved)]
    [InlineData(EVoucherRedemptionStatus.Redeemed)]
    public async Task
    Evaluate_counts_active_redemption_toward_total_limit(
        EVoucherRedemptionStatus status)
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync();

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.MaxTotalUses = 1;
        await context.SaveChangesAsync();

        await AddRedemptionAsync(
            context,
            voucher,
            product,
            user,
            status,
            now);

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E244]", result.Message);
    }

    [Fact]
    public async Task
        Evaluate_ignores_released_redemption_for_total_limit()
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync();

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.MaxTotalUses = 1;
        await context.SaveChangesAsync();

        await AddRedemptionAsync(
            context,
            voucher,
            product,
            user,
            EVoucherRedemptionStatus.Released,
            now);

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.True(result.IsEligible);
    }
    [Theory]
    [InlineData(EVoucherRedemptionStatus.Reserved)]
    [InlineData(EVoucherRedemptionStatus.Redeemed)]
    public async Task
    Evaluate_counts_active_redemption_toward_user_limit(
        EVoucherRedemptionStatus status)
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync();

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.MaxUsesPerUser = 1;
        await context.SaveChangesAsync();

        await AddRedemptionAsync(
            context,
            voucher,
            product,
            user,
            status,
            now);

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E245]", result.Message);
    }

    [Fact]
    public async Task
        Evaluate_does_not_count_another_users_redemption_toward_user_limit()
    {
        var now = new DateTime(
            2026,
            9,
            5,
            12,
            0,
            0);

        await using var context =
            await CreateContextAsync();

        var currentUser =
            await context.Users.SingleAsync();

        var anotherUser = new User
        {
            UserName = "another-redemption@test.com",
            Email = "another-redemption@test.com"
        };

        context.Users.Add(anotherUser);
        await context.SaveChangesAsync();

        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        voucher.MaxUsesPerUser = 1;
        await context.SaveChangesAsync();

        await AddRedemptionAsync(
            context,
            voucher,
            product,
            anotherUser,
            EVoucherRedemptionStatus.Redeemed,
            now);

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            currentUser.Id,
            now);

        Assert.True(result.IsEligible);
    }
    [Fact]
    public async Task Evaluate_rejects_inactive_voucher()
    {
        await using var context =
            await CreateContextAsync(voucher =>
            {
                voucher.IsActive = false;
            });

        var user = await context.Users.SingleAsync();
        var product = await context.Products.SingleAsync();
        var voucher = await context.Vouchers.SingleAsync();

        var service =
            new VoucherEligibilityService(context);

        var result = await service.EvaluateAsync(
            voucher,
            product,
            user.Id,
            DateTime.Now);

        Assert.False(result.IsEligible);
        Assert.Contains("[E240]", result.Message);
    }

    private static async Task AddRedemptionAsync(
                            AppDbContext context,
                            Voucher voucher,
                            Product product,
                            User user,
                            EVoucherRedemptionStatus status,
                            DateTime now)
    {
        var order = new Order
        {
            UserId = user.Id,

            ProductId = product.Id,
            Product = product,

            VoucherId = voucher.Id,
            Voucher = voucher,

            VoucherCodeSnapshot =
                voucher.Code,

            VoucherDiscountTypeSnapshot =
                voucher.DiscountType,

            VoucherValueSnapshot =
                voucher.Value,

            OriginalPrice = product.Price,
            DiscountAmount = voucher.Value,
            Total = product.Price - voucher.Value,

            AccessDurationMonths =
                product.AccessDurationMonths,

            Status = status switch
            {
                EVoucherRedemptionStatus.Redeemed =>
                    EOrderStatus.Paid,

                EVoucherRedemptionStatus.Released =>
                    EOrderStatus.Canceled,

                _ =>
                    EOrderStatus.WaintingPayment
            }
        };

        var redemption = new VoucherRedemption
        {
            VoucherId = voucher.Id,
            Voucher = voucher,

            Order = order,

            UserId = user.Id,
            Status = status,
            ReservedAt = now,

            RedeemedAt =
                status == EVoucherRedemptionStatus.Redeemed
                    ? now
                    : null,

            ReleasedAt =
                status == EVoucherRedemptionStatus.Released
                    ? now
                    : null
        };

        context.Orders.Add(order);
        context.VoucherRedemptions.Add(redemption);

        await context.SaveChangesAsync();
    }
    private static async Task<AppDbContext>
        CreateContextAsync(
            Action<Voucher>? configureVoucher = null)
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"DimaTests-{Guid.NewGuid()}")
                .Options;

        var context = new AppDbContext(options);

        var user = new User
        {
            UserName = "eligibility@test.com",
            Email = "eligibility@test.com"
        };

        var product = new Product
        {
            Title = "Plano de teste",
            Description =
                "Produto para teste de elegibilidade",
            Price = 100m,
            IsActive = true,
            AccessDurationMonths = 1
        };

        context.Users.Add(user);
        context.Products.Add(product);

        await context.SaveChangesAsync();

        var voucher = new Voucher
        {
            Code = "ELIGIBLE25",
            Title = "Voucher de elegibilidade",
            Description =
                "Voucher utilizado nos testes",
            DiscountType =
                EVoucherDiscountType.FixedAmount,
            Value = 25m,
            IsActive = true
        };

        configureVoucher?.Invoke(voucher);

        context.Vouchers.Add(voucher);
        await context.SaveChangesAsync();

        return context;
    }
}