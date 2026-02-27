using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests
{
    public abstract class PagedRequest
    {
        public string UserId { get; set; } = string.Empty;
        public int PageNumber { get; set; } = Configuration.DefaultPageNumber;
        public int PageSize { get; set; } = Configuration.DefaultPageSize;
    }
}
