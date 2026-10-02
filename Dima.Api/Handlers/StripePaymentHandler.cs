using Dima.Api.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Stripe;
using Stripe.Checkout;
using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Dima.Api.Configuration;
using Microsoft.Extensions.Options;
using Dima.Core.Models.Payments;
using Dima.Core.Common;

namespace Dima.Api.Handlers
{
    public class StripePaymentHandler(
        AppDbContext context,
        TimeProvider timeProvider,
        IOptions<OrderExpirationOptions> expirationOptions, IOptions<ApiOptions> apiOptions, IStripeClient stripeClient, ILogger<StripePaymentHandler>? logger = null)
        : IPaymentHandler
    {
        private readonly ILogger<StripePaymentHandler> _logger = logger ?? NullLogger<StripePaymentHandler>.Instance;

        public async Task<Response<PaymentSessionResult?>> CreateSessionAsync(
            CreatePaymentSessionRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiOptions.Value.StripeApiKey))
                {
                    return new Response<PaymentSessionResult?>(
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
                    return new Response<PaymentSessionResult?>(
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
                    return new Response<PaymentSessionResult?>(
                        null,
                        404,
                        "[E194] Pedido nao encontrado");
                }

                if (order.Status != EOrderStatus.WaitingPayment)
                {
                    return new Response<PaymentSessionResult?>(
                        null,
                        400,
                        "[E195] Pedido nao esta aguardando pagamento");
                }
                var service = new SessionService(stripeClient);

                if (!string.IsNullOrWhiteSpace(order.PaymentSessionId))
                {
                    var existingSession = await service.GetAsync(
                        order.PaymentSessionId);

                    if (existingSession.Status == "complete")
                    {
                        return new Response<PaymentSessionResult?>(
                            null,
                            409,
                            "[E253] O checkout deste pedido já foi concluído. " +
                            "Consulte Meus pedidos para acompanhar a confirmação.");
                    }

                    if (existingSession.Status == "expired")
                    {
                        return new Response<PaymentSessionResult?>(
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
                        return new Response<PaymentSessionResult?>(
                            null,
                            409,
                            "[E255] A sessão deste pedido não está disponível " +
                            "para pagamento. Consulte Meus pedidos.");
                    }

                    return new Response<PaymentSessionResult?>(
                        new PaymentSessionResult
                        {
                            SessionId = existingSession.Id,
                            RedirectUrl = existingSession.Url!,
                            ExpiresAt = new DateTimeOffset(
                                DateTime.SpecifyKind(
                                    existingSession.ExpiresAt,
                                    DateTimeKind.Utc)),
                            Gateway = EPaymentGateway.Stripe
                        });
                }
                var nowUtc = timeProvider.GetUtcNow();

                if (order.PaymentSessionExpiresAt is null)
                {
                    if (order.ExpiresAt.HasValue &&
                        order.ExpiresAt.Value <= nowUtc)
                    {
                        return new Response<PaymentSessionResult?>(
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
                        return new Response<PaymentSessionResult?>(
                            null,
                            409,
                            "[E256] O pedido foi atualizado durante a preparação " +
                            "do pagamento. Atualize a página e tente novamente.");
                    }
                }
                else if (order.PaymentSessionExpiresAt.Value <= nowUtc)
                {
                    return new Response<PaymentSessionResult?>(
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
                        $"{apiOptions.Value.FrontendUrl}/pedidos/" +
                        $"{order.Number}/confirmar",

                    CancelUrl =
                        $"{apiOptions.Value.FrontendUrl}/pedidos/" +
                        $"{order.Number}"
                };

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey =
                        $"dima:checkout:v1:{order.Id}:{order.Number}:" +
                        $"{order.PaymentSessionExpiresAt.Value.ToUnixTimeSeconds()}"
                };
                var checkoutCorrelationId = RequestCorrelation.ForPaymentAttempt(requestOptions.IdempotencyKey);
                options.Metadata[RequestCorrelation.StripeMetadataKey] = checkoutCorrelationId;
                options.PaymentIntentData.Metadata[RequestCorrelation.StripeMetadataKey] = checkoutCorrelationId;

                Session session;
                try
                {
                    session = await service.CreateAsync(options, requestOptions);
                }
                catch (StripeException exception) when (exception.StripeError?.Type == "idempotency_error")
                {
                    // An attempt created before DT-19 may be cached without this metadata.
                    // Retry its original parameters with the same key; never create a new attempt.
                    _logger.LogWarning(exception, "Checkout anterior sem metadados de correlação: {OrderNumber}", order.Number);
                    options.Metadata.Remove(RequestCorrelation.StripeMetadataKey);
                    options.PaymentIntentData.Metadata.Remove(RequestCorrelation.StripeMetadataKey);
                    session = await service.CreateAsync(options, requestOptions);
                }
                _logger.LogInformation("Checkout Stripe criado: pedido {OrderNumber}, sessão {PaymentSessionId}, correlação {CheckoutCorrelationId}",
                    order.Number, session.Id, checkoutCorrelationId);

                // Recupera alterações que possam ter ocorrido enquanto
                // aguardávamos a resposta do Stripe.
                await context.Entry(order).ReloadAsync();

                if (context.Entry(order).State == EntityState.Detached)
                {
                    return new Response<PaymentSessionResult?>(
                        null,
                        409,
                        "[E258] O pedido não está mais disponível. " +
                        "Não foi possível concluir a abertura do checkout.");
                }

                if (!string.IsNullOrWhiteSpace(order.PaymentSessionId) &&
                    order.PaymentSessionId != session.Id)
                {
                    return new Response<PaymentSessionResult?>(
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
                order.Gateway = EPaymentGateway.Stripe;

                // O prazo inicial para abrir o checkout passa a ser
                // o vencimento da sessão efetivamente criada.
                if (order.Status == EOrderStatus.WaitingPayment)
                {
                    order.ExpiresAt = confirmedExpiration;
                }

                try
                {
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    return new Response<PaymentSessionResult?>(
                        null,
                        409,
                        "[E260] O pedido foi atualizado durante a abertura " +
                        "do checkout. Atualize a página para verificar a situação.");
                }

                if (order.Status != EOrderStatus.WaitingPayment)
                {
                    return new Response<PaymentSessionResult?>(
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
                    return new Response<PaymentSessionResult?>(
                        null,
                        409,
                        "[E255] A sessão deste pedido não está disponível " +
                        "para pagamento. Consulte Meus pedidos.");
                }

                return new Response<PaymentSessionResult?>(
                        new PaymentSessionResult
                        {
                            SessionId = currentSession.Id,
                            RedirectUrl = currentSession.Url!,
                            ExpiresAt = new DateTimeOffset(
                                DateTime.SpecifyKind(
                                    currentSession.ExpiresAt,
                                    DateTimeKind.Utc)),
                            Gateway = EPaymentGateway.Stripe
                        });
            }
            catch (StripeException ex)
            {
                _logger.LogOperationError(ex);

                return new Response<PaymentSessionResult?>(
                    null,
                    502,
                    "[E090] Não foi possível iniciar o pagamento no Stripe");
            }
            catch (Exception ex)
            {
                _logger.LogOperationError(ex);

                return new Response<PaymentSessionResult?>(
                    null,
                    500,
                    "[E091] Falha interna ao criar sessão de pagamento");
            }
        }
        public async Task<Response<string?>> RefundAsync(
            string externalReference,
            string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(apiOptions.Value.StripeApiKey))
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

                var service = new RefundService(stripeClient);

                var requestOptions = new RequestOptions
                {
                    IdempotencyKey = idempotencyKey
                };

                var refund = await service.CreateAsync(
                    options,
                    requestOptions);
                _logger.LogInformation("Reembolso Stripe solicitado: pagamento {PaymentIntentId}, reembolso {RefundId}",
                    externalReference, refund.Id);

                return new Response<string?>(
                    refund.Id,
                    200,
                    "Reembolso solicitado ao Stripe com sucesso");
            }
            catch (StripeException ex)
            {
                _logger.LogOperationError(ex);

                return new Response<string?>(
                    null,
                    502,
                    "[E217] Não foi possível solicitar o reembolso no Stripe");
            }
            catch (Exception ex)
            {
                _logger.LogOperationError(ex);

                return new Response<string?>(
                    null,
                    500,
                    "[E218] Falha interna ao solicitar reembolso");
            }
        }
        public async Task<Response<bool>> CloseSessionAsync(
    string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return new Response<bool>(
                    false,
                    400,
                    "[E262] Sessão de pagamento não informada.");
            }

            if (string.IsNullOrWhiteSpace(apiOptions.Value.StripeApiKey))
            {
                return new Response<bool>(
                    false,
                    500,
                    "[E263] StripeApiKey não configurada.");
            }

            try
            {
                var service = new SessionService(stripeClient);

                var session = await service.GetAsync(sessionId);

                if (session.Status == "open" &&
                    session.PaymentStatus == "unpaid")
                {
                    session = await service.ExpireAsync(sessionId);
                }

                if (session.Status == "expired" &&
                    session.PaymentStatus == "unpaid")
                {
                    return new Response<bool>(
                        true,
                        200,
                        "Sessão expirada e sem pagamento.");
                }

                return new Response<bool>(
                    false,
                    409,
                    "[E264] A sessão não permite liberar a reserva. " +
                    "O pagamento pode estar concluído ou em processamento.");
            }
            catch (StripeException ex)
            {
                _logger.LogOperationError(ex);

                return new Response<bool>(
                    false,
                    502,
                    "[E265] Não foi possível confirmar o encerramento " +
                    "da sessão de pagamento. Tente novamente.");
            }
        }
    }
}
