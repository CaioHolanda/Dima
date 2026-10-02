using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Payments;
using Dima.Core.Responses;
using Dima.Core.Requests.Payment;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Stripe
{
    public class CreateSessionEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/session", HandleAsync)
            .Produces<Response<PaymentSessionResult?>>()
            .Produces<Response<PaymentSessionResult?>>(StatusCodes.Status400BadRequest);
        private static async Task<IResult> HandleAsync(
            ClaimsPrincipal user,
            IPaymentHandler handler,
            CreatePaymentSessionRequest request)
        {
            request.UserId = user.Identity!.Name ?? string.Empty;
            var result = await handler.CreateSessionAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}
