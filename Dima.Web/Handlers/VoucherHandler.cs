using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using Microsoft.Extensions.Http;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class VoucherHandler(IHttpClientFactory httpClientFactory) : IVoucherHandler
    {
        private readonly HttpClient _client=httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<Voucher?>> GetByCodeAsync(
            GetVoucherByCodeRequest request)
        {
            var code = Uri.EscapeDataString(
                request.Code.Trim().ToUpperInvariant());

            var response = await _client.GetAsync(
                $"v1/vouchers/{code}");

            if (!response.IsSuccessStatusCode)
            {
                return new Response<Voucher?>(
                    null,
                    (int)response.StatusCode,
                    $"[E064] Não foi possível obter o voucher. Status: {response.StatusCode}");
            }

            var result =
                await response.Content.ReadFromJsonAsync<Response<Voucher?>>();

            return result ??
                new Response<Voucher?>(
                    null,
                    400,
                    "[E079] Resposta vazia da API");
        }
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
