using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class PayOrderRequest:Request
    {
        public long Id { get; set; }
        public string ExternalReference { get; set; } = string.Empty;
    }
}
