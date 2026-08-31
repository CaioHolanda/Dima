using Dima.Core.Requests.Payment;
using Dima.Core.Responses;


namespace Dima.Core.Handlers;

public interface IPaymentHandler
{
    Task<Response<string?>> CreateSessionAsync(
        CreatePaymentSessionRequest request);
    Task<Response<string?>> RefundAsync(
        string externalReference,
        string idempotencyKey);

}