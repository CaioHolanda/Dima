using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using System.Net.Http.Json;
using Dima.Core.Models.Payments;

namespace Dima.Web.Handlers;

public class StripePaymentHandler(IHttpClientFactory httpClientFactory) : IPaymentCheckoutClient
{
    private readonly HttpClient _client = httpClientFactory.CreateClient(Configuration.HttpClientName);

    public async Task<Response<PaymentSessionResult?>> CreateSessionAsync(CreatePaymentSessionRequest request)
    {
        using var response = await _client.PostAsJsonAsync("v1/payments/stripe/session", request);
        return await HttpResponseReader.ReadAsync<PaymentSessionResult?>(response,
            "[E080] Não foi possível criar a sessão de pagamento");
    }
}
