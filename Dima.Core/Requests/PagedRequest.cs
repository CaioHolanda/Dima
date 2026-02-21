using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests
{
    public abstract class PagedRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
