using Dima.Core.Requests.Payment;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Dima.Core.Responses.Payment;

namespace Dima.Core.Handlers;

public interface IPaymentHandler
{
    Task<Response<string?>> CreateSessionAsync(
        CreatePaymentSessionRequest request);

    Task<Response<List<PaymentTransactionResponse>>>
        GetTransactionsByOrderNumberAsync(
            GetTransactionsByOrderNumberRequest request);
}