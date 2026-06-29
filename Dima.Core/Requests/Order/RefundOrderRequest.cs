using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class RefundOrderRequest:Request
    {
        public long Id { get; set; }
    }
}
