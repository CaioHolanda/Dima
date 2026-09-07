using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class CancelOrderEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
            => app.MapPost("/{id}/cancel", HandleAsync)
                    .WithName("Orders: Cancel Order")
                    .WithSummary("Cancela um pedido")
                    .WithDescription("Cancela um pedido")
                    .WithOrder(1)
                    .Produces<Response<Order?>>(StatusCodes.Status200OK)
                    .Produces<Response<Order?>>(StatusCodes.Status400BadRequest)
                    .Produces<Response<Order?>>(StatusCodes.Status404NotFound)
                    .Produces<Response<Order?>>(StatusCodes.Status409Conflict)
                    .Produces<Response<Order?>>(StatusCodes.Status500InternalServerError);
        private static async Task<IResult> HandleAsync(
            IOrderHandler handler,
            long id,
            ClaimsPrincipal user)
        {
            var request = new CancelOrderRequest{
                Id = id,
                UserId=user.Identity!.Name ?? string.Empty
            };
            var result = await handler.CancelAsync(request);
            if (result.IsSuccess)
            {
                return TypedResults.Ok(result);
            }

            return TypedResults.Json(
                result,
                statusCode: result.Code);
        }
    }
}
