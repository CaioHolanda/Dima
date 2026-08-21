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

    public async Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request)
    {
        var response = await _client.GetFromJsonAsync<
            PagedResponse<List<AdminOrderListItem>?>>(
            $"v1/admin/orders" +
            $"?pageNumber={request.PageNumber}" +
            $"&pageSize={request.PageSize}");

        return response ??
            new PagedResponse<List<AdminOrderListItem>?>(
                null,
                400,
                "[E191] Não foi possível listar os pedidos");
    }
}