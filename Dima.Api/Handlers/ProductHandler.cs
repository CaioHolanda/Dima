using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class ProductHandler(AppDbContext context) : IProductHandler
    {
        public async Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request)
        {
            try
            {
                var query = context.Products.AsNoTracking()
                                            .Where(x => x.IsActive == true)
                                            .OrderByDescending(x => x.Price);

                var products = await query.Skip((request.PageNumber - 1) * request.PageSize)
                                            .Take(request.PageSize)
                                            .ToListAsync();

                var count = await query.CountAsync();
                return new PagedResponse<List<Product>?>(products, count, request.PageNumber, request.PageSize);
            }
            catch 
            {
                return new PagedResponse<List<Product>?>(null, 500, "Nao foi possivel consultar os produtos");
            }
        }

        public async Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request)
        {
            try
            {
                var product = await context.Products  .AsNoTracking()
                                                .FirstOrDefaultAsync
                                                (x => x.Slug == request.Slug && x.IsActive == true);

                return product is null 
                        ? new Response<Product?>(null, 404, "Produto nao encontrado")
                        : new Response<Product?>(product);
            }
            catch
            {
                return new Response<Product?>(null, 500, "Nao foi possivel buscar produto");
            }
        }
    }
}
