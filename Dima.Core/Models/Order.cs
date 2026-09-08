using Dima.Core.Enums;
using Dima.Core.Models.Vouchers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Dima.Core.Models
{
    public class Order
    {
        public long Id { get; set; }
        [JsonIgnore]
        public byte[] RowVersion { get; set; } = [];
        public string Number { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public long? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }
        public string? VoucherCodeSnapshot { get; set; }
        public EVoucherDiscountType? VoucherDiscountTypeSnapshot { get; set; }
        public decimal? VoucherValueSnapshot { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public int AccessDurationMonths { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTimeOffset? ExpiresAt { get; set; }
        public DateTimeOffset? ExpiredAt { get; set; }
        public string? PaymentSessionId { get; set; }
        public DateTimeOffset? PaymentSessionExpiresAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? AccessStartsAt { get; set; }
        public DateTime? AccessEndsAt { get; set; }
        public string? ExternalReference { get; set; }
        public EPaymentGateway Gateway { get; set; } = EPaymentGateway.Stripe;
        public EOrderStatus Status { get; set; } = EOrderStatus.WaintingPayment;
        public long UserId { get; set; }
        public string? RefundReference { get; set; }
        public string? RefundFailureReason { get; set; }
        public DateTime? RefundedAt { get; set; }
        public ERefundReason? RefundReason { get; set; }
        public string? RefundReasonDetails { get; set; }
    }
}
