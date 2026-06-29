using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Order
{
    public class GetOrderByNumberRequest:Request
    {
        public string Number { get; set; } = string.Empty;
    }
}
