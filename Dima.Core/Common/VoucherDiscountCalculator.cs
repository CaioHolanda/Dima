using Dima.Core.Enums;
using Dima.Core.Models.Vouchers;

namespace Dima.Core.Common;

public static class VoucherDiscountCalculator
{
    public static decimal Calculate(
        decimal originalPrice,
        Voucher? voucher)
    {
        if (originalPrice <= 0 || voucher is null)
            return 0m;

        var discount = voucher.DiscountType switch
        {
            EVoucherDiscountType.FixedAmount =>
                voucher.Value,

            EVoucherDiscountType.Percentage =>
                originalPrice * voucher.Value / 100m,

            _ => 0m
        };

        discount = decimal.Round(
            discount,
            2,
            MidpointRounding.AwayFromZero);

        return Math.Min(
            originalPrice,
            Math.Max(0m, discount));
    }
}