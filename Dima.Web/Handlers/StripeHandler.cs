using Dima.Core.Handlers;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Dima.Core.Responses.Stripe;
using System.Net.Http.Json;
using System.Text.Json;

namespace Dima.Web.Handlers
{
    public class StripeHandler(IHttpClientFactory httpClientFactory) : IStripeHandler
    {
        private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);

        public async Task<Response<string?>> CreateSessionAsync(
            CreateSessionRequest request)
        {
            using var response =
                await _client.PostAsJsonAsync(
                    "v1/payments/stripe/session",
                    request);

            var content =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new Response<string?>(
                    null,
                    (int)response.StatusCode,
                    $"Falha ao criar sessão Stripe: {content}");
            }

            return JsonSerializer.Deserialize<Response<string?>>(
                       content,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? new Response<string?>(
                       null,
                       400,
                       "[E080] Resposta inválida da API");
        }

        public async Task<Response<List<StripeTransactionResponse>>> GetTransactionsByOrderNumberAsync(GetTransactionsByOrderNumberRequest request)
        {
            var result = await _client.PostAsJsonAsync($"v1/payments/stripe/{request.Number}/transactions", request);
            return await result.Content.ReadFromJsonAsync<Response<List<StripeTransactionResponse>>>()
                ?? new Response<List<StripeTransactionResponse>>(null, 400, "[E083] Falha ao consultar as transacoes do pedido");
        }
    }
}
