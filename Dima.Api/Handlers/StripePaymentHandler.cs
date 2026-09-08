using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Stripe;
using Stripe.Checkout;
using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;
using CoreConfiguration = Dima.Core.Configuration;
using Dima.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Dima.Api.Handlers
{
    public class StripePaymentHandler(
        AppDbContext context,
        TimeProvider timeProvider,
        IOptions<OrderExpirationOptions> expirationOptions)
        : IPaymentHandler
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
                var service = new SessionService();

                if (!string.IsNullOrWhiteSpace(order.PaymentSessionId))
                {
                    var existingSession = await service.GetAsync(
                        order.PaymentSessionId);

                    if (existingSession.Status == "complete")
                    {
                        return new Response<string?>(
                            null,
                            409,
                            "[E253] O checkout deste pedido já foi concluído. " +
                            "Consulte Meus pedidos para acompanhar a confirmação.");
                    }

                    if (existingSession.Status == "expired")
                    {
                        return new Response<string?>(
                            null,
                            409,
                            "[E254] A sessão de pagamento deste pedido expirou. " +
                            "Consulte Meus pedidos para verificar a situação.");
                    }

                    var sessionIsAvailable =
                        existingSession.Status == "open" &&
                        existingSession.PaymentStatus == "unpaid" &&
                        existingSession.ExpiresAt >
                            timeProvider.GetUtcNow().UtcDateTime &&
                        !string.IsNullOrWhiteSpace(existingSession.Url);

                    if (!sessionIsAvailable)
                    {
                        return new Response<string?>(
                            null,
                            409,
                            "[E255] A sessão deste pedido não está disponível " +
                            "para pagamento. Consulte Meus pedidos.");
                    }

                    return new Response<string?>(
                        existingSession.Url);
                }
                var nowUtc = timeProvider.GetUtcNow();

                if (order.PaymentSessionExpiresAt is null)
                {
                    if (order.ExpiresAt.HasValue &&
                        order.ExpiresAt.Value <= nowUtc)
                    {
                        return new Response<string?>(
                            null,
                            409,
                            "[E252] O prazo de pagamento deste pedido terminou. " +
                            "Consulte Meus pedidos para verificar a situação.");
                    }

                    var plannedExpiration = nowUtc.AddMinutes(
                        expirationOptions.Value.PaymentSessionLifetimeMinutes);

                    // O Stripe utiliza timestamps com precisão de segundos.
                    order.PaymentSessionExpiresAt =
                        DateTimeOffset.FromUnixTimeSeconds(
                            plannedExpiration.ToUnixTimeSeconds());

                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        return new Response<string?>(
                            null,
                            409,
                            "[E256] O pedido foi atualizado durante a preparação " +
                            "do pagamento. Atualize a página e tente novamente.");
                    }
                }
                else if (order.PaymentSessionExpiresAt.Value <= nowUtc)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E257] O prazo da tentativa de pagamento terminou. " +
                        "A situação da sessão precisa ser verificada.");
                }
                var options = new SessionCreateOptions
                {
                    ClientReferenceId = order.Number,

                    ExpiresAt = order.PaymentSessionExpiresAt.Value.UtcDateTime,

                    Metadata = new Dictionary<string, string>
                    {
                        ["order"] = order.Number,
                        ["userId"] = order.UserId.ToString()
                    },
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
                                        Name = $"Dima - Pedido {order.Number}",
                                        Description =
                                            $"Plano com duração de {order.AccessDurationMonths} mês(es)"                                
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
                        $"{order.Number}"
                };

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey =
                        $"dima:checkout:v1:{order.Id}:{order.Number}:" +
                        $"{order.PaymentSessionExpiresAt.Value.ToUnixTimeSeconds()}"
                };

                var session = await service.CreateAsync(
                    options,
                    requestOptions);

                // Recupera alterações que possam ter ocorrido enquanto
                // aguardávamos a resposta do Stripe.
                await context.Entry(order).ReloadAsync();

                if (context.Entry(order).State == EntityState.Detached)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E258] O pedido não está mais disponível. " +
                        "Não foi possível concluir a abertura do checkout.");
                }

                if (!string.IsNullOrWhiteSpace(order.PaymentSessionId) &&
                    order.PaymentSessionId != session.Id)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E259] O pedido já está associado a outra sessão " +
                        "de pagamento. A situação precisa ser verificada.");
                }

                var confirmedExpiration = new DateTimeOffset(
                    DateTime.SpecifyKind(
                        session.ExpiresAt,
                        DateTimeKind.Utc));

                order.PaymentSessionId = session.Id;
                order.PaymentSessionExpiresAt = confirmedExpiration;

                // O prazo inicial para abrir o checkout passa a ser
                // o vencimento da sessão efetivamente criada.
                if (order.Status == EOrderStatus.WaintingPayment)
                {
                    order.ExpiresAt = confirmedExpiration;
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E260] O pedido foi atualizado durante a abertura " +
                        "do checkout. Atualize a página para verificar a situação.");
                }

                if (order.Status != EOrderStatus.WaintingPayment)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E261] A situação do pedido mudou. " +
                        "Consulte Meus pedidos antes de continuar.");
                }

                // Uma resposta idempotente pode conter o estado original
                // da criação. Consultamos o estado atual antes de usar a URL.
                var currentSession = await service.GetAsync(session.Id);

                var existingSessionIsAvailable =
                    currentSession.Status == "open" &&
                    currentSession.PaymentStatus == "unpaid" &&
                    currentSession.ExpiresAt >
                        timeProvider.GetUtcNow().UtcDateTime &&
                    !string.IsNullOrWhiteSpace(currentSession.Url);

                if (!existingSessionIsAvailable)
                {
                    return new Response<string?>(
                        null,
                        409,
                        "[E255] A sessão deste pedido não está disponível " +
                        "para pagamento. Consulte Meus pedidos.");
                }

                return new Response<string?>(currentSession.Url);
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
            string externalReference,
            string idempotencyKey)
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

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = idempotencyKey
                };

                var refund = await service.CreateAsync(
                    options,
                    requestOptions);

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
