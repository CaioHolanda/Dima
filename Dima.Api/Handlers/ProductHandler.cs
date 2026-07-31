using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class ProductHandler(AppDbContext context) : IProductHandler
    {
        public async Task<Response<Product?>> CreateAsync(
                                                CreateProductRequest request)
        {
            try
            {
                var slugExists = await context.Products
                    .AnyAsync(x => x.Slug == request.Slug);

                if (slugExists)
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status409Conflict,
                        "[E114] Já existe um produto com este slug");

                var product = new Product
                {
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    Price = request.Price,
                    Slug = request.Slug,
                    IsActive = request.IsActive
                };

                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    201,
                    "Produto criado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    500,
                    "[E115] Não foi possível criar o produto");
            }
        }
        public async Task<Response<Product?>> UpdateAsync(
                                                UpdateProductRequest request)
        {
            try
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (product is null)
                    return new Response<Product?>(
                        null,
                        404,
                        "[E116] Produto não encontrado");

                var slugExists = await context.Products
                    .AnyAsync(x =>
                        x.Slug == request.Slug &&
                        x.Id != request.Id);

                if (slugExists)
                    return new Response<Product?>(
                        null,
                        400,
                        "[E117] Já existe outro produto com este slug");

                product.Title = request.Title;
                product.Description = request.Description;
                product.Price = request.Price;
                product.Slug = request.Slug;
                product.IsActive = request.IsActive;

                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    200,
                    "Produto atualizado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    500,
                    "[E118] Não foi possível atualizar o produto");
            }
        }
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

        public async Task<Response<Product?>> DeactivateAsync(
            DeactivateProductRequest request)
        {
            try
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (product is null)
                    return new Response<Product?>(
                        null,
                        404,
                        "[E119] Produto não encontrado");

                product.IsActive = false;

                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    200,
                    "Produto desativado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    500,
                    "[E120] Não foi possível desativar o produto");
            }
        }
    }
}
