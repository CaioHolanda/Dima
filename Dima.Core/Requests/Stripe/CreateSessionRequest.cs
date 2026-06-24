using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Stripe
{
    public class CreateSessionRequest:Request
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string ProductTitle { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public long OrderTotal { get; set; }
    }
}
