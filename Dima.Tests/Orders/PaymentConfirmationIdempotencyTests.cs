using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Tests.Orders.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Orders;

public class PaymentConfirmationIdempotencyTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Repeated_confirmation_preserves_purchase_and_voucher_redemption(
        bool withVoucher, bool differentReference)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DT17-{Guid.NewGuid()}").Options;
        await using var db = new AppDbContext(options);
        var user = new User { UserName = "payment@test.com", Email = "payment@test.com" };
        var product = new Product
        {
            Title = "Plano", Description = "Plano", Slug = "plano", Price = 100m,
            IsActive = true, AccessDurationMonths = 6
        };
        db.Users.Add(user);
        db.Products.Add(product);
        var voucher = new Voucher { Code = "DT17", Title = "Desconto", Value = 10m };
        if (withVoucher) db.Vouchers.Add(voucher);
        await db.SaveChangesAsync();
        var order = new Order
        {
            ProductId = product.Id, UserId = user.Id, OriginalPrice = 100m,
            DiscountAmount = withVoucher ? 10m : 0m, Total = withVoucher ? 90m : 100m,
            AccessDurationMonths = 6, Gateway = EPaymentGateway.Stripe,
            Status = EOrderStatus.WaintingPayment, VoucherId = withVoucher ? voucher.Id : null
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        if (withVoucher)
        {
            db.VoucherRedemptions.Add(new VoucherRedemption
            {
                OrderId = order.Id, VoucherId = voucher.Id, UserId = user.Id,
                Status = EVoucherRedemptionStatus.Reserved, ReservedAt = DateTime.UtcNow.AddMinutes(-5)
            });
            await db.SaveChangesAsync();
        }
        var number = order.Number;
        var amount = (long)(order.Total * 100);
        db.ChangeTracker.Clear();
        var first = await Handler(db).ConfirmPaymentAsync(number, "pi_dt17", amount, "brl", user.Id.ToString());
        Assert.True(first.IsSuccess);
        Assert.Equal(200, first.Code);
        var before = await db.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(EOrderStatus.Paid, before.Status);
        Assert.Equal("pi_dt17", before.ExternalReference);
        Assert.NotNull(before.PaidAt);
        Assert.NotNull(before.AccessStartsAt);
        Assert.Equal(before.AccessStartsAt.Value.AddMonths(6), before.AccessEndsAt);
        var redemptionBefore = await db.VoucherRedemptions.AsNoTracking().SingleOrDefaultAsync();
        if (withVoucher)
        {
            Assert.NotNull(redemptionBefore);
            Assert.Equal(EVoucherRedemptionStatus.Redeemed, redemptionBefore.Status);
            Assert.NotNull(redemptionBefore.RedeemedAt);
        }

        // A separate context represents a later delivery and proves persisted state is sufficient.
        await using var secondDb = new AppDbContext(options);
        var repeated = await Handler(secondDb).ConfirmPaymentAsync(number,
            differentReference ? "pi_other" : "pi_dt17", amount, "brl", user.Id.ToString());
        Assert.Equal(!differentReference, repeated.IsSuccess);
        Assert.Equal(differentReference ? 409 : 200, repeated.Code);
        if (differentReference) Assert.Contains("[E206]", repeated.Message);
        var after = await secondDb.Orders.AsNoTracking().SingleAsync();
        Assert.Equal(before.Status, after.Status);
        Assert.Equal(before.ExternalReference, after.ExternalReference);
        Assert.Equal(before.PaidAt, after.PaidAt);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
        Assert.Equal(before.AccessStartsAt, after.AccessStartsAt);
        Assert.Equal(before.AccessEndsAt, after.AccessEndsAt);
        Assert.Equal(before.OriginalPrice, after.OriginalPrice);
        Assert.Equal(before.DiscountAmount, after.DiscountAmount);
        Assert.Equal(before.Total, after.Total);
        var redemptions = await secondDb.VoucherRedemptions.AsNoTracking().ToListAsync();
        if (withVoucher)
        {
            var redemptionAfter = Assert.Single(redemptions);
            Assert.Equal(redemptionBefore!.Id, redemptionAfter.Id);
            Assert.Equal(redemptionBefore.Status, redemptionAfter.Status);
            Assert.Equal(redemptionBefore.ReservedAt, redemptionAfter.ReservedAt);
            Assert.Equal(redemptionBefore.RedeemedAt, redemptionAfter.RedeemedAt);
            Assert.Equal(redemptionBefore.ReleasedAt, redemptionAfter.ReleasedAt);
        }
        else Assert.Empty(redemptions);
    }

    private static OrderHandler Handler(AppDbContext db) => new(db, new FakePaymentHandler(),
        new VoucherEligibilityService(db), Options.Create(new OrderExpirationOptions()), TimeProvider.System, TestBusinessTime.Create(TimeProvider.System));
}
