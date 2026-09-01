using Dima.Core.Enums;
using System.Text.Json.Serialization;

namespace Dima.Core.Models.Vouchers;

public class Voucher
{
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public EVoucherDiscountType DiscountType { get; set; }
        = EVoucherDiscountType.FixedAmount;

    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public int? MaxTotalUses { get; set; }
    public int? MaxUsesPerUser { get; set; }

    public long? AssignedUserId { get; set; }

    public long? ProductId { get; set; }
    public Product? Product { get; set; }

    public bool IsActive { get; set; } = true;
    [JsonIgnore]
    public List<VoucherRedemption> Redemptions { get; set; } = [];
}