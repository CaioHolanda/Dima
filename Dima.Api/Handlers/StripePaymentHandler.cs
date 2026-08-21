using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Dima.Core.Responses.Payment;
using Stripe;
using Stripe.Checkout;
using CoreConfiguration = Dima.Core.Configuration;

namespace Dima.Api.Handlers
{
    public class StripePaymentHandler : IPaymentHandler
    {
        public async Task<Response<string?>> CreateSessionAsync(
            CreatePaymentSessionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ApiConfiguration.StripeApiKey))
                {
                    return new Response<string?>(
                        null,
                        500,
                        "[E089] StripeApiKey não configurada");
                }

                var options = new SessionCreateOptions
                {
                    CustomerEmail = request.UserId,

                    PaymentIntentData =
                        new SessionPaymentIntentDataOptions
                        {
                            Metadata = new Dictionary<string, string>
                            {
                                ["order"] = request.OrderNumber
                            }
                        },

                    PaymentMethodTypes = ["card"],

                    LineItems =
                    [
                        new SessionLineItemOptions
                {
                    PriceData =
                        new SessionLineItemPriceDataOptions
                        {
                            Currency = "brl",
                            ProductData =
                                new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = request.ProductTitle,
                                    Description =
                                        request.ProductDescription
                                },
                            UnitAmount = request.OrderTotal
                        },

                    Quantity = 1
                }
                    ],

                    Mode = "payment",

                    SuccessUrl =
                        $"{CoreConfiguration.FrontendUrl}/pedidos/" +
                        $"{request.OrderNumber}/confirmar",

                    CancelUrl =
                        $"{CoreConfiguration.FrontendUrl}/pedidos/" +
                        $"{request.OrderNumber}/cancelar"
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                return new Response<string?>(session.Url);
            }
            catch (StripeException ex)
            {
                Console.WriteLine(
                    $"[STRIPE CREATE SESSION] {ex.Message}");

                return new Response<string?>(
                    null,
                    502,
                    $"[E090] Falha no Stripe: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[STRIPE CREATE SESSION] {ex}");

                return new Response<string?>(
                    null,
                    500,
                    "[E091] Falha interna ao criar sessão de pagamento");
            }
        }
        public async Task<Response<List<PaymentTransactionResponse>>>
            GetTransactionsByOrderNumberAsync(
                GetTransactionsByOrderNumberRequest request)
        {
            var options = new PaymentIntentSearchOptions
            {
                Query=$"metadata['order']:'{request.Number}'"
            };
            var service = new PaymentIntentService();
            var data = new List<PaymentTransactionResponse>();
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
                return new Response<List<PaymentTransactionResponse>>(null, 404, "[E082] Nenhuma transacao encontrada");

            foreach (var item in transactions)
            {
                data.Add(new PaymentTransactionResponse
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
            return new Response<List<PaymentTransactionResponse>>(data);
        }
    }
}
