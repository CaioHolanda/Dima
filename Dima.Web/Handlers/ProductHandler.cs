using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class ProductHandler(IHttpClientFactory httpClientFactory) : IProductHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request)
        {
            var result = await _client.GetFromJsonAsync<PagedResponse<List<Product>?>>("v1/products");
            if (result is null)
                return new PagedResponse<List<Product>?>(null, 400, "[E065] Nao foi possivel obter os produtos");
            return result;
        }

        public async Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request)
        {
            var result = await _client.GetFromJsonAsync<Response<Product?>>($"v1/products/{request.Slug}");
            if (result is null)
                return new Response<Product?>(null, 400, "[E066] Nao foi possivel obter o produto");
            return result;
        }
    }
}
