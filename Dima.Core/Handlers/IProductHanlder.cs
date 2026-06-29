using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Handlers
{
    public interface IProductHandler
    {
        Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request);
        Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request);
    }
}
