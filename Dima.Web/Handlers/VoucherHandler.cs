using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using Microsoft.Extensions.Http;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class VoucherHandler(IHttpClientFactory httpClientFactory) : IVoucherHandler
    {
        private readonly HttpClient _client=httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<VoucherApplication?>> ApplyAsync(
            ApplyVoucherRequest request)
        {
            var response = await _client.PostAsJsonAsync(
                "v1/vouchers/apply",
                request);

            var result =
                await response.Content
                    .ReadFromJsonAsync<
                        Response<VoucherApplication?>>();

            return result ??
                new Response<VoucherApplication?>(
                    null,
                    (int)response.StatusCode,
                    "[E235] Resposta vazia ao aplicar o voucher");
        }

    }
}
