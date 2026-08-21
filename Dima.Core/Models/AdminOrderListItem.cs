using Dima.Core.Enums;

namespace Dima.Core.Models;

public class AdminOrderListItem
{
    public long Id { get; set; }
    public string Number { get; set; } = string.Empty;

    public long UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;

    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public string? VoucherCode { get; set; }

    public decimal OriginalPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? AccessStartsAt { get; set; }
    public DateTime? AccessEndsAt { get; set; }

    public EOrderStatus Status { get; set; }
}