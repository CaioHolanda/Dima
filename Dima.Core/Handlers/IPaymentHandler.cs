using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Dima.Core.Models.Payments;


namespace Dima.Core.Handlers;

public interface IPaymentHandler
{
    Task<Response<PaymentSessionResult?>> CreateSessionAsync(
        CreatePaymentSessionRequest request);
    Task<Response<string?>> RefundAsync(
        string externalReference,
        string idempotencyKey);
    Task<Response<bool>> CloseSessionAsync(
    string sessionId);

}