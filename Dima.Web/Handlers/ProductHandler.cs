using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Web.Handlers
{
    public class ProductHandler : IProductHandler
    {
        public Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
