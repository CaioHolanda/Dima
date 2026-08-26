using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Stripe;

namespace Dima.Api.Endpoints.Stripe
{
    public class WebhookEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
            => app.MapPost("/webhook", HandleAsync)
                .WithName("Stripe Webhook")
                .WithSummary("Receive Stripe webhook events")
                .WithDescription("Receives events sent by Stripe")
                .Produces(StatusCodes.Status200OK)
                .Produces(StatusCodes.Status400BadRequest);

        private static async Task<IResult> HandleAsync(HttpRequest request,             
            IOrderPaymentConfirmationHandler orderHandler,
            ILogger<WebhookEndpoint> logger)
        {
            if (string.IsNullOrWhiteSpace(ApiConfiguration.StripeWebhookSecret))
                return Results.Problem(
                    "[E196] StripeWebhookSecret nao configurado",
                    statusCode: StatusCodes.Status500InternalServerError);

            using var reader = new StreamReader(request.Body);
            var json = await reader.ReadToEndAsync();

            if (!request.Headers.TryGetValue(
                    "Stripe-Signature",
                    out var stripeSignature))
            {
                return Results.BadRequest(
                    "[E197] Assinatura Stripe nao encontrada");
            }

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    ApiConfiguration.StripeWebhookSecret);
                logger.LogInformation(
                    "Stripe webhook recebido: {EventType} - {EventId}",
                    stripeEvent.Type,
                    stripeEvent.Id);

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;

                    if (paymentIntent is null)
                    {
                        return Results.BadRequest(
                            "[E199] PaymentIntent invalido");
                    }

                    if (!paymentIntent.Metadata.TryGetValue(
                            "order",
                            out var orderNumber))
                    {
                        logger.LogWarning(
                        "Stripe PaymentIntent {PaymentIntentId} recebido sem metadata de pedido",
                        paymentIntent.Id);
                        return Results.BadRequest(
                            "[E200] Numero do pedido nao encontrado no evento");
                    }
                    var result = await orderHandler.ConfirmPaymentAsync(
                                                    orderNumber,
                                                    paymentIntent.Id);

                    if (!result.IsSuccess)
                    {
                        return Results.Problem(
                            result.Message,
                            statusCode: result.Code);
                    }
                    logger.LogInformation(
                        "Pagamento Stripe confirmado - Pedido: {OrderNumber}, PaymentIntent: {PaymentIntentId}",
                        orderNumber,
                        paymentIntent.Id);
                }

                return Results.Ok();
            }
            catch (StripeException ex)
            {
                logger.LogWarning(ex,"Webhook Stripe rejeitado por assinatura invalida");
                return Results.BadRequest(
                    "[E198] Assinatura Stripe invalida");
            }
        }
    }
}