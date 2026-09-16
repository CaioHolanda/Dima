using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers;

public class AdminOrderHandler(
    IHttpClientFactory httpClientFactory)
    : IAdminOrderHandler
{
    private readonly HttpClient _client =
        httpClientFactory.CreateClient(
            Configuration.HttpClientName);

    public async Task<Response<AdminOrderDetails?>> GetByIdAsync(long id)
    {
        using var response = await _client.GetAsync($"v1/admin/orders/{id}");
        return await response.Content.ReadFromJsonAsync<Response<AdminOrderDetails?>>()
            ?? new Response<AdminOrderDetails?>(null, (int)response.StatusCode, "Não foi possível consultar o pedido.");
    }

    public async Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request)
    {
        var response = await _client.GetFromJsonAsync<
            PagedResponse<List<AdminOrderListItem>?>>(
            $"v1/admin/orders" +
            AdminQuery.Build(request));

        return response ??
            new PagedResponse<List<AdminOrderListItem>?>(
                null,
                400,
                "[E191] Não foi possível listar os pedidos");
    }
}