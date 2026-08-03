using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Handlers
{
    public interface IAdminProductHandler
    {
        Task<Response<Product?>> CreateAsync(CreateProductRequest request);
        Task<Response<Product?>> UpdateAsync(UpdateProductRequest request);
        Task<Response<Product?>> DeactivateAsync(DeactivateProductRequest request);
        Task<Response<Product?>> GetByIdForAdminAsync(GetProductByIdRequest request);
        Task<PagedResponse<List<Product>?>> GetAllForAdminAsync(GetAllAdminProductsRequest request);
    }
}
