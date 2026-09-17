using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers;

public class OrderHandler(IHttpClientFactory httpClientFactory) : IOrderHandler
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);

    public async Task<Response<Order?>> CancelAsync(CancelOrderRequest request)
    {
        using var response = await _client.PostAsJsonAsync($"v1/orders/{request.Id}/cancel", request);
        return await HttpResponseReader.ReadAsync<Order?>(response, "[E067] Não foi possível cancelar o pedido");
    }

    public async Task<Response<Order?>> CreateAsync(CreateOrderRequest request)
    {
        using var response = await _client.PostAsJsonAsync("v1/orders/", request);
        return await HttpResponseReader.ReadAsync<Order?>(response, "[E068] Não foi possível cadastrar pedido");
    }

    public async Task<PagedResponse<List<Order>?>> GetAllAsync(GetAllOrdersRequest request)
    {
        using var response = await _client.GetAsync($"v1/orders/?pageNumber={request.PageNumber}&pageSize={request.PageSize}");
        return await HttpResponseReader.ReadPagedAsync<List<Order>?>(response, "[E071] Não foi possível listar os pedidos");
    }

    public async Task<Response<Order?>> GetByNumberAsync(GetOrderByNumberRequest request)
    {
        using var response = await _client.GetAsync($"v1/orders/{Uri.EscapeDataString(request.Number)}");
        return await HttpResponseReader.ReadAsync<Order?>(response, "[E072] Não foi possível encontrar o pedido");
    }

    public async Task<Response<Order?>> RefundAsync(RefundOrderRequest request)
    {
        using var response = await _client.PostAsJsonAsync($"v1/orders/{request.Id}/refund", request);
        return await HttpResponseReader.ReadAsync<Order?>(response, "[E070] Não foi possível estornar o produto");
    }
}
