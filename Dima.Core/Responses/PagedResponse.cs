using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
// testando um commit a partir do escritorio

namespace Dima.Core.Responses
{
    public class PagedResponse<TData>:Response<TData>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = Configuration.DefaultPageSize;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        [JsonConstructor]
        public PagedResponse(
            TData? data,
            int totalCount,
            int currentPage=1,
            int pageSize = Configuration.DefaultPageSize) : base(data)
        {
            Data = data;
            TotalCount = totalCount;
            CurrentPage = currentPage;
            PageSize = pageSize;
        }
        public PagedResponse(
            TData? data,
            int code=Configuration.DefaultStatusCode,
            string? message = null) : base(data, code, message) 
        {
        }
    }
}
