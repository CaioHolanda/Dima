using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Security.Claims;
using Dima.Api.Services;
using Dima.Core.Enums;

namespace Dima.Api.Endpoints.Orders
{
    public class GetOrderByNumberEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{number}", HandleAsync)
            .WithName("Order: By number")
            .WithSummary("Read one order")
            .WithDescription("Read one order")
            .WithOrder(4)
            .Produces<Response<Order?>>();

        private static async Task<IResult> HandleAsync(
            ClaimsPrincipal user,
            IOrderHandler handler,
            OrderExpirationService expirationService,
            string number)
        {
            var request = new GetOrderByNumberRequest
            {
                UserId = user.Identity!.Name ?? string.Empty,
                Number = number
            };

            // Confirma que o pedido pertence ao usuário autenticado.
            var result = await handler.GetByNumberAsync(request);

            if (!result.IsSuccess)
                return TypedResults.Json(result, statusCode: result.Code);

            if (result.Data is null)
                return TypedResults.NotFound();

            if (result.Data.Status != EOrderStatus.WaintingPayment)
                return TypedResults.Ok(result);

            var expirationResult =
                await expirationService.ExpireAsync(result.Data.Id);

            // Reconsulta para refletir a expiração ou uma atualização concorrente.
            result = await handler.GetByNumberAsync(request);

            if (!result.IsSuccess)
                return TypedResults.Json(result, statusCode: result.Code);

            if (!expirationResult.IsSuccess)
            {
                result.Message =
                    "Não foi possível concluir a verificação deste pedido. " +
                    "O estado exibido é o último confirmado. " +
                    "Atualize a página para tentar novamente.";
            }

            return TypedResults.Ok(result);
        }
    }
}