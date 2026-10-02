using Dima.Core.Enums;

namespace Dima.Core.Models.Vouchers;

public class AdminVoucherListItem
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public EVoucherDiscountType DiscountType { get; set; }
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public long? AssignedUserId { get; set; }
    public string? AssignedUserEmail { get; set; }

    public bool IsActive { get; set; }
}