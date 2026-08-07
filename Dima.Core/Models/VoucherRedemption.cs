using Dima.Core.Enums;

namespace Dima.Core.Models
{
    public class VoucherRedemption
    {
        public long Id { get; set; }

        public long VoucherId { get; set; }
        public Voucher Voucher { get; set; } = null!;

        public long OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public EVoucherRedemptionStatus Status { get; set; }

        public DateTime ReservedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
    }
}