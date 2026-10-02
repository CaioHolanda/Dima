using Dima.Core.Models.Payments;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;

namespace Dima.Web.Handlers;

public interface IPaymentCheckoutClient
{
    Task<Response<PaymentSessionResult?>> CreateSessionAsync(
        CreatePaymentSessionRequest request);
}