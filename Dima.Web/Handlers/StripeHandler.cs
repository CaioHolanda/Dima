using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using System.Net.Http.Json;
using System.Text.Json;
using Dima.Core.Models.Payments;

namespace Dima.Web.Handlers;

public class StripePaymentHandler(
    IHttpClientFactory httpClientFactory) : IPaymentCheckoutClient
{
    private readonly HttpClient _client =
        httpClientFactory.CreateClient(Configuration.HttpClientName);

    public async Task<Response<PaymentSessionResult?>> CreateSessionAsync(
        CreatePaymentSessionRequest request)
    {
        using var response =
            await _client.PostAsJsonAsync(
                "v1/payments/stripe/session",
                request);

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return new Response<PaymentSessionResult?>(
                null,
                (int)response.StatusCode,
                $"Falha ao criar sessão Stripe: {content}");
        }

        return JsonSerializer.Deserialize<Response<PaymentSessionResult?>>(
                   content,
                   new JsonSerializerOptions
                   {
                       PropertyNameCaseInsensitive = true
                   })
               ?? new Response<PaymentSessionResult?>(
                   null,
                   400,
                   "[E080] Resposta inválida da API");
    }
}