using Dima.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models
{
    public class Order
    {
        public long Id { get; set; }
        public string Number { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public long ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public long? VoucherId { get; set; }
        public Voucher? Voucher { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
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
    }
}
