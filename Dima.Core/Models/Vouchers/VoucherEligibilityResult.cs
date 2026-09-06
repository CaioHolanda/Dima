namespace Dima.Core.Models.Vouchers;

public sealed record VoucherEligibilityResult(bool IsEligible,string Message)
{
    public static VoucherEligibilityResult Eligible()
        => new(
            true,
            "Voucher elegível");

    public static VoucherEligibilityResult Rejected(
        string message)
        => new(
            false,
            message);
}