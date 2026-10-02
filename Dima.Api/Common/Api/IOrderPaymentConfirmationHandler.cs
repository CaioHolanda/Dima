
using Dima.Core.Models;
using Dima.Core.Responses;

namespace Dima.Api.Common.Api
{
    public interface IOrderPaymentConfirmationHandler
    {
        Task<Response<Order?>> ConfirmPaymentAsync(
            string orderNumber,
            string externalReference,
            long amountReceived,
            string currency,
            string paymentUserId);

        Task<Response<Order?>> ConfirmRefundAsync(
            string paymentIntentId,
            string refundId,
            string refundStatus,
            string? failureReason);
    }
}