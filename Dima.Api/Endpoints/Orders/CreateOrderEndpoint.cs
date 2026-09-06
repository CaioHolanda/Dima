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
                    .WithOrder(2)
                    .Produces<Response<Order?>>(StatusCodes.Status201Created)
                    .Produces<Response<Order?>>(StatusCodes.Status400BadRequest)
                    .Produces<Response<Order?>>(StatusCodes.Status404NotFound)
                    .Produces<Response<Order?>>(StatusCodes.Status409Conflict)
                    .Produces<Response<Order?>>(StatusCodes.Status500InternalServerError);
        private static async Task<IResult> HandleAsync(
            IOrderHandler handler,
            CreateOrderRequest request,
            ClaimsPrincipal user)
        {
            request.UserId = user.Identity!.Name ?? string.Empty;

            var result = await handler.CreateAsync(request);

            if (result.IsSuccess)
            {
                return TypedResults.Created(
                    $"v1/orders/{result.Data?.Number}",
                    result);
            }

            return TypedResults.Json(
                result,
                statusCode: result.Code);
        }
    }
}