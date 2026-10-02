using Azure;
using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class RefundOrderEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        =>
            app.MapPost("/{id}/refund", HandleAsync)
                .RequireAuthorization()
                .WithName("Refund order by Id")
                .WithSummary("Refund by Id")
                .WithDescription("Refund by Id")
                .WithOrder(6)
                .Produces<Response<Order?>>(StatusCodes.Status200OK)
                .Produces<Response<Order?>>(StatusCodes.Status400BadRequest)
                .Produces<Response<Order?>>(StatusCodes.Status404NotFound)
                .Produces<Response<Order?>>(StatusCodes.Status409Conflict)
                .Produces<Response<Order?>>(StatusCodes.Status500InternalServerError);
        private static async Task<IResult> HandleAsync(
            IOrderHandler handler,
            long id,
            ClaimsPrincipal user,
            RefundOrderRequest request
            )
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return TypedResults.Unauthorized();
            }
            request.Id = id;
            request.UserId = user.Identity!.Name??string.Empty;
            var result = await handler.RefundAsync(request);

            if (result.IsSuccess)
                return TypedResults.Ok(result);

            return TypedResults.Json(
                result,
                statusCode: result.Code);
        }
    }
}
