namespace Dima.Core.Models.Vouchers;

public class VoucherApplication
{
    public long VoucherId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
}