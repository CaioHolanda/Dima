using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class GetVoucherByCodeRequest:Request
    {
        public string Code { get; set; } = string.Empty;
    }
}
