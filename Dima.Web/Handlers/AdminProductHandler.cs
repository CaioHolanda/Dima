using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers;

public class AdminProductHandler(
    IHttpClientFactory httpClientFactory)
    : IAdminProductHandler
{
    private readonly HttpClient _client =
        httpClientFactory.CreateClient(
            Configuration.HttpClientName);

    public async Task<PagedResponse<List<Product>?>>
        GetAllForAdminAsync(
            GetAllAdminProductsRequest request)
    {
        var response = await _client.GetAsync(
            $"v1/admin/products" +
            $"?pageNumber={request.PageNumber}" +
            $"&pageSize={request.PageSize}");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<List<Product>?>>();

        return result ??
            new PagedResponse<List<Product>?>(
                null,
                (int)response.StatusCode,
                "[E121] Não foi possível obter os produtos");
    }

    public async Task<Response<Product?>>
        GetByIdForAdminAsync(
            GetProductByIdRequest request)
    {
        var response = await _client.GetAsync(
            $"v1/admin/products/{request.Id}");

        return await ReadResponseAsync(response);
    }

    public async Task<Response<Product?>>
        CreateAsync(CreateProductRequest request)
    {
        var response = await _client.PostAsJsonAsync(
            "v1/admin/products",
            request);

        return await ReadResponseAsync(response);
    }

    public async Task<Response<Product?>>
        UpdateAsync(UpdateProductRequest request)
    {
        var response = await _client.PutAsJsonAsync(
            $"v1/admin/products/{request.Id}",
            request);

        return await ReadResponseAsync(response);
    }

    public async Task<Response<Product?>>
        ActivateAsync(
            ActivateProductRequest request)
    {
        var response = await _client.PutAsync(
            $"v1/admin/products/{request.Id}/activate",
            null);

        return await ReadResponseAsync(response);
    }

    public async Task<Response<Product?>>
        DeactivateAsync(
            DeactivateProductRequest request)
    {
        var response = await _client.DeleteAsync(
            $"v1/admin/products/{request.Id}");

        return await ReadResponseAsync(response);
    }

    private static async Task<Response<Product?>>
        ReadResponseAsync(HttpResponseMessage response)
    {
        var result = await response.Content
            .ReadFromJsonAsync<Response<Product?>>();

        return result ??
            new Response<Product?>(
                null,
                (int)response.StatusCode,
                "[E122] Não foi possível processar o produto");
    }

}