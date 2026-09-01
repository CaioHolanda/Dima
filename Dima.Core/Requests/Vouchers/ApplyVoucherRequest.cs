namespace Dima.Core.Requests.Vouchers;

public class ApplyVoucherRequest : Request
{
    public string Code { get; set; } = string.Empty;
    public long ProductId { get; set; }
}