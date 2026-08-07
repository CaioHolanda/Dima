using Dima.Core.Enums;

namespace Dima.Core.Models
{
    public class Voucher
    {
        public long Id { get; set; }

        public string Number { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public EVoucherDiscountType DiscountType { get; set; }
            = EVoucherDiscountType.FixedAmount;
        
        public decimal Value { get; set; }

        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }

        public int? MaxTotalUses { get; set; }
        public int? MaxUsesPerUser { get; set; }

        public string? AssignedUserId { get; set; }
        public long? ProductId { get; set; }
        public Product? Product { get; set; }

        public bool IsActive { get; set; } = true;

        public List<VoucherRedemption> Redemptions { get; set; } = [];
    }
}