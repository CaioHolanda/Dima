using Dima.Core.Enums;

namespace Dima.Core.Models;

public class AdminOrderDetails : AdminOrderListItem
{
    public DateTime UpdatedAt { get; set; }
    public EPaymentGateway? Gateway { get; set; }
    public string? ExternalReference { get; set; }
    public string? PaymentSessionId { get; set; }
    public DateTimeOffset? PaymentSessionExpiresAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public EVoucherDiscountType? VoucherDiscountTypeSnapshot { get; set; }
    public decimal? VoucherValueSnapshot { get; set; }
    public int AccessDurationMonths { get; set; }
}