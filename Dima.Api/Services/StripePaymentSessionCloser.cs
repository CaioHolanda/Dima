using Dima.Api.Common.Api;
using Dima.Core.Responses;
using Stripe;
using Stripe.Checkout;

namespace Dima.Api.Services;

public sealed class StripePaymentSessionCloser(
    SessionService sessionService,
    ILogger<StripePaymentSessionCloser> logger)
    : IPaymentSessionCloser
{
    public async Task<Response<bool>> CloseAsync(
        string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new Response<bool>(
                false,
                400,
                "[E262] Sessão de pagamento não informada.");
        }

        if (string.IsNullOrWhiteSpace(
                ApiConfiguration.StripeApiKey))
        {
            return new Response<bool>(
                false,
                500,
                "[E263] StripeApiKey não configurada.");
        }

        try
        {
            var session = await sessionService.GetAsync(
                sessionId);

            if (session.Status == "open" &&
                session.PaymentStatus == "unpaid")
            {
                session = await sessionService.ExpireAsync(
                    sessionId);
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
            logger.LogWarning(
                ex,
                "Falha ao encerrar a sessão Stripe {SessionId}",
                sessionId);

            return new Response<bool>(
                false,
                502,
                "[E265] Não foi possível confirmar o encerramento " +
                "da sessão de pagamento. Tente novamente.");
        }
    }
}