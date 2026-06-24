using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Stripe
{
    public class GetTransactionsByOrderNumberRequest:Request
    {
        public string Number { get; set; } = string.Empty;
    }
}
