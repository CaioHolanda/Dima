using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Stripe;
using Stripe.Checkout;
using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;
using CoreConfiguration = Dima.Core.Configuration;

namespace Dima.Api.Handlers
{
    public class StripePaymentHandler(AppDbContext context) : IPaymentHandler
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
                var user = await context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Email == request.UserId ||
                        x.UserName == request.UserId);

                if (user is null)
                {
                    return new Response<string?>(
                        null,
                        404,
                        "[E193] Usuario nao encontrado");
                }

                var order = await context.Orders
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .FirstOrDefaultAsync(x =>
                        x.Number == request.OrderNumber &&
                        x.UserId == user.Id);

                if (order is null)
                {
                    return new Response<string?>(
                        null,
                        404,
                        "[E194] Pedido nao encontrado");
                }

                if (order.Status != EOrderStatus.WaintingPayment)
                {
                    return new Response<string?>(
                        null,
                        400,
                        "[E195] Pedido nao esta aguardando pagamento");
                }
                var options = new SessionCreateOptions
                {
                    CustomerEmail = user.Email,

                    PaymentIntentData =
                        new SessionPaymentIntentDataOptions
                        {
                            Metadata = new Dictionary<string, string>
                            {
                                ["order"] = order.Number,
                                ["userId"] = user.Id.ToString()
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
                                    Name = order.Product.Title,
                                    Description = order.Product.Description
                                },
                            UnitAmount = (long)Math.Round(order.Total * 100, 0)
                        },

                    Quantity = 1
                }
                    ],

                    Mode = "payment",

                    SuccessUrl =
                        $"{CoreConfiguration.FrontendUrl}/pedidos/" +
                        $"{order.Number}/confirmar",

                    CancelUrl =
                        $"{CoreConfiguration.FrontendUrl}/pedidos/" +
                        $"{order.Number}/cancelar"
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
        public async Task<Response<string?>> RefundAsync(
            string externalReference)
        {
            if (string.IsNullOrWhiteSpace(ApiConfiguration.StripeApiKey))
            {
                return new Response<string?>(
                    null,
                    500,
                    "[E215] StripeApiKey não configurada");
            }

            if (string.IsNullOrWhiteSpace(externalReference))
            {
                return new Response<string?>(
                    null,
                    400,
                    "[E216] Referencia externa do pagamento nao informada");
            }

            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = externalReference,
                    Reason = "requested_by_customer"
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                return new Response<string?>(
                    refund.Id,
                    200,
                    "Reembolso solicitado ao Stripe com sucesso");
            }
            catch (StripeException ex)
            {
                Console.WriteLine(
                    $"[STRIPE REFUND] {ex.Message}");

                return new Response<string?>(
                    null,
                    502,
                    $"[E217] Falha no Stripe: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[STRIPE REFUND] {ex}");

                return new Response<string?>(
                    null,
                    500,
                    "[E218] Falha interna ao solicitar reembolso");
            }
        }
    }
}
