using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class CreateOrderEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
            => app.MapPost("/", HandleAsync)
                    .WithName("Orders: Create Order")
                    .WithSummary("Cria um pedido")
                    .WithDescription("Cria um pedido")
                    .WithOrder(1)
                    .Produces<Response<Order?>>();
        private static async Task<IResult> HandleAsync(
            IOrderHandler handler,
            CreateOrderRequest request,
            ClaimsPrincipal user)
        {
            request.UserId = user.Identity!.Name ?? string.Empty;

            var result = await handler.CreateAsync(request);
            return result.IsSuccess
                ? TypedResults.Created($"v1/orders/{result.Data?.Number}",result)
                : TypedResults.BadRequest(result);
        }
    }
}