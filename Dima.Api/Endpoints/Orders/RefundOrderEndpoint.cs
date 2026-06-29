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
                .WithName("Refund order by Id")
                .WithSummary("Refund by Id")
                .WithDescription("Refund by Id")
                .WithOrder(6)
                .Produces<Response<Order?>>();
        
        private static async Task<IResult> HandleAsync(
            IOrderHandler handler,
            long id,
            ClaimsPrincipal user,
            RefundOrderRequest request
            )
        {
            request.Id = id;
            request.UserId = user.Identity!.Name??string.Empty;
            var result = await handler.RefundAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}
