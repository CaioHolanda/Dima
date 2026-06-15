using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class GetOrderByNumberEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/{number}", HandleAsync)
            .WithName("Order: By number")
            .WithSummary("Read one order")
            .WithDescription("Read one order")
            .WithOrder(6)
            .Produces<Response<Order?>>();

        private static async Task<IResult> HandleAsync(
            ClaimsPrincipal user,
            IOrderHandler handler,
            string number)
        {
            var request = new GetOrderByNumberRequest
            {
                UserId = user.Identity!.Name ?? string.Empty,
                Number = number
            };
            var result = await handler.GetByNumberAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}