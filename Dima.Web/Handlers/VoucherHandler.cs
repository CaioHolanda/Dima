using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.Extensions.Http;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class VoucherHandler(IHttpClientFactory httpClientFactory) : IVoucherHandler
    {
        private readonly HttpClient _client=httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<Voucher?>> GetByNumberAsync(GetVoucherByNumberRequest request)
        =>await _client.GetFromJsonAsync<Response<Voucher?>>($"v1/voucher/{request.Number}")
          ?? new Response<Voucher?>(null, 400, "[E064] Nao foi possivel obter o voucher");
    }
}
