using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Services;

public class VoucherEligibilityService(
    AppDbContext context)
{
    public async Task<VoucherEligibilityResult>
        EvaluateAsync(
            Voucher voucher,
            Product product,
            long userId,
            DateTime now)
    {
        if (!voucher.IsActive)
        {
            return VoucherEligibilityResult.Rejected(
                "[E240] Voucher inativo");
        }

        var currentDate = now.Date;

        if (voucher.StartsAt.HasValue &&
            currentDate < voucher.StartsAt.Value.Date)
        {
            return VoucherEligibilityResult.Rejected(
                "[E241] Voucher ainda não está vigente");
        }

        if (voucher.EndsAt.HasValue &&
            currentDate > voucher.EndsAt.Value.Date)
        {
            return VoucherEligibilityResult.Rejected(
                "[E242] Voucher expirado");
        }

        if (voucher.AssignedUserId.HasValue &&
            voucher.AssignedUserId.Value != userId)
        {
            return VoucherEligibilityResult.Rejected(
                "[E243] Voucher não atribuído a este usuário");
        }

        if (voucher.ProductId.HasValue &&
            voucher.ProductId.Value != product.Id)
        {
            return VoucherEligibilityResult.Rejected(
                "[E233] Voucher não aplicável a este produto");
        }

        if (voucher.DiscountType ==
                EVoucherDiscountType.FixedAmount &&
            voucher.Value > product.Price)
        {
            return VoucherEligibilityResult.Rejected(
                "[E229] O valor do voucher é superior ao valor do produto");
        }

        var activeRedemptions =
            context.VoucherRedemptions
                .AsNoTracking()
                .Where(x =>
                    x.VoucherId == voucher.Id &&
                    (x.Status ==
                        EVoucherRedemptionStatus.Reserved ||
                     x.Status ==
                        EVoucherRedemptionStatus.Redeemed));

        if (voucher.MaxTotalUses.HasValue)
        {
            var totalUses =
                await activeRedemptions.CountAsync();

            if (totalUses >= voucher.MaxTotalUses.Value)
            {
                return VoucherEligibilityResult.Rejected(
                    "[E244] O limite total de utilizações do voucher foi atingido");
            }
        }

        if (voucher.MaxUsesPerUser.HasValue)
        {
            var userUses =
                await activeRedemptions.CountAsync(x =>
                    x.UserId == userId);

            if (userUses >=
                voucher.MaxUsesPerUser.Value)
            {
                return VoucherEligibilityResult.Rejected(
                    "[E245] O limite de utilizações deste voucher pelo usuário foi atingido");
            }
        }

        return VoucherEligibilityResult.Eligible();
    }
}