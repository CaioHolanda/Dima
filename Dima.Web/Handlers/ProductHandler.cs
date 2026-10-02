using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Web.Handlers;

public class ProductHandler(IHttpClientFactory httpClientFactory) : IProductHandler
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);

    public async Task<PagedResponse<List<Product>?>> GetAllAsync(GetAllProductsRequest request)
    {
        using var response = await _client.GetAsync(
            $"v1/products?pageNumber={request.PageNumber}&pageSize={request.PageSize}");
        return await HttpResponseReader.ReadPagedAsync<List<Product>?>(response,
            "[E065] Não foi possível obter os produtos");
    }

    public async Task<Response<Product?>> GetBySlugAsync(GetProductBySlugRequest request)
    {
        using var response = await _client.GetAsync($"v1/products/{Uri.EscapeDataString(request.Slug)}");
        return await HttpResponseReader.ReadAsync<Product?>(response,
            "[E066] Não foi possível obter o produto");
    }
}
