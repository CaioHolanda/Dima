using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class PayOrderRequest:Request
    {
        public string Number { get; set; } = string.Empty;
        public string ExternalReference { get; set; } = string.Empty;
    }
}
