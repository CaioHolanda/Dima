using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Requests.Stripe;
using Dima.Core.Responses;
using Dima.Core.Responses.Stripe;
using Stripe;
using Stripe.Checkout;

namespace Dima.Api.Handlers
{
    public class StripeHanlder : IStripeHandler
    {
        public async Task<Response<string?>> CreateSessionAsync(CreateSessionRequest request)
        {
            var options = new SessionCreateOptions
            {
                CustomerEmail = request.UserId,
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    Metadata = new Dictionary<string, string>
                    {
                        {"order",request.OrderNumber }
                    }
                },
                PaymentMethodTypes = 
                [
                    "card"
                ],
                LineItems = [
                    new SessionLineItemOptions{
                        PriceData = new SessionLineItemPriceDataOptions{
                            Currency="brl",
                            ProductData = new SessionLineItemPriceDataProductDataOptions{
                                Name = request.ProductTitle,
                                Description = request.ProductDescription
                            },
                            UnitAmount=request.OrderTotal,
                        },
                        Quantity=1
                    }
                    ],
                Mode="payment",
                SuccessUrl=$"{Configuration.FrontendUrl}/pedidos/{request.OrderNumber}/confirmar",
                CancelUrl=$"{Configuration.FrontendUrl}/pedidos/{request.OrderNumber}/cancelar"
            };
            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return new Response<string?>(session.Id);
        }

        public async Task<Response<List<StripeTransactionResponse>>> GetTransactionsByOrderNumberAsync(GetTransactionsByOrderNumberRequest request)
        {
            var options = new PaymentIntentSearchOptions
            {
                Query=$"metadata['order']:'{request.Number}'"
            };
            var service = new PaymentIntentService();
            var data = new List<StripeTransactionResponse>();
            var list = await service.ListAsync(new PaymentIntentListOptions
            {
                Limit = 20
            });

            var transactions = list.Data
                .Where(x =>
                    x.Metadata is not null &&
                    x.Metadata.TryGetValue("order", out var order) &&
                    order == request.Number)
                .ToList();

            if (transactions.Count == 0)
                return new Response<List<StripeTransactionResponse>>(null, 404, "[E082] Nenhuma transacao encontrada");

            foreach (var item in transactions)
            {
                data.Add(new StripeTransactionResponse
                {
                    Id = item.Id,
                    Email = item.ReceiptEmail,
                    Amount = item.Amount,
                    AmountCaptures = item.AmountReceived,
                    Status = item.Status,
                    Paid = item.Status == "succeeded",
                    Refunded = false
                });
            }
            return new Response<List<StripeTransactionResponse>>(data);
        }
    }
}
