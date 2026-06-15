using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class GetVoucherByNumberRequest:Request
    {
        public string Number { get; set; } = string.Empty;
    }
}
