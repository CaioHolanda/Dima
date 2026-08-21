using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Dima.Api.Handlers
{
    public class ProductHandler(AppDbContext context) : IProductHandler, IAdminProductHandler
    {
        public async Task<Response<Product?>> CreateAsync(
            CreateProductRequest request)
        {
            try
            {
                if (request.AccessDurationMonths is <= 0)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E172] A duração do acesso deve ser maior que zero");
                }
                if (string.IsNullOrWhiteSpace(request.Slug))
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E126] O slug é obrigatório");
                }

                var slug = request.Slug.Trim();

                if (!SlugPattern.IsMatch(slug))
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E126] O slug deve conter apenas letras minúsculas, números e hífens");
                }

                var slugExists = await context.Products
                    .AnyAsync(x => x.Slug == slug);

                if (slugExists)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status409Conflict,
                        "[E114] Já existe um produto com este slug");
                }

                var product = new Product
                {
                    Title = request.Title,
                    Description = request.Description ?? string.Empty,
                    Price = request.Price,
                    Slug = slug,
                    IsActive = request.IsActive,
                    AccessDurationMonths = request.AccessDurationMonths
                };

                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    StatusCodes.Status201Created,
                    "Produto criado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E115] Não foi possível criar o produto");
            }
        }
        public async Task<Response<Product?>> UpdateAsync(
            UpdateProductRequest request)
        {
            try
            {
                if (request.AccessDurationMonths is <= 0)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E173] A duração do acesso deve ser maior que zero");
                }

                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (product is null)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E116] Produto não encontrado");
                }

                if (string.IsNullOrWhiteSpace(request.Slug))
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E128] O slug é obrigatório");
                }

                var slug = request.Slug.Trim();

                if (!SlugPattern.IsMatch(slug))
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E127] O slug deve conter apenas letras minúsculas, números e hífens");
                }

                var slugExists = await context.Products
                    .AnyAsync(x =>
                        x.Slug == slug &&
                        x.Id != request.Id);

                if (slugExists)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status409Conflict,
                        "[E117] Já existe outro produto com este slug");
                }

                product.Title = request.Title;
                product.Description = request.Description ?? string.Empty;
                product.Price = request.Price;
                product.Slug = slug;
                product.IsActive = request.IsActive;
                product.AccessDurationMonths = request.AccessDurationMonths;

                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    StatusCodes.Status200OK,
                    "Produto atualizado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    StatusCodes.Status500InternalServerError,
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
        public async Task<PagedResponse<List<Product>?>> GetAllForAdminAsync(GetAllAdminProductsRequest request)
        {
            try
            {
                var query = context.Products
                    .AsNoTracking()
                    .OrderByDescending(x => x.IsActive)
                    .ThenByDescending(x => x.Price);

                var products = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var count = await query.CountAsync();

                return new PagedResponse<List<Product>?>(
                    products,
                    count,
                    request.PageNumber,
                    request.PageSize);
            }
            catch
            {
                return new PagedResponse<List<Product>?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E123] Não foi possível consultar os produtos");
            }
        }
        public async Task<Response<Product?>> GetByIdForAdminAsync(GetProductByIdRequest request)
        {
            try
            {
                var product = await context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                return product is null
                    ? new Response<Product?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E124] Produto não encontrado")
                    : new Response<Product?>(product);
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E125] Não foi possível consultar o produto");
            }
        }

        public async Task<Response<Product?>> ActivateAsync(ActivateProductRequest request)
        {
            try
            {
                var product = await context.Products
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (product is null)
                {
                    return new Response<Product?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E130] Produto não encontrado");
                }

                product.IsActive = true;

                await context.SaveChangesAsync();

                return new Response<Product?>(
                    product,
                    StatusCodes.Status200OK,
                    "Produto ativado com sucesso");
            }
            catch
            {
                return new Response<Product?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E131] Não foi possível ativar o produto");
            }
        }

        private static readonly Regex SlugPattern = new("^[a-z0-9]+(?:-[a-z0-9]+)*$",
                RegexOptions.Compiled);
    }
}
