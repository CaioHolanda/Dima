using Dima.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class RefundOrderRequest:Request
    {
        public long Id { get; set; }
        public ERefundReason? RefundReason { get; set; }
        public string? RefundReasonDetails { get; set; }
    }
}
