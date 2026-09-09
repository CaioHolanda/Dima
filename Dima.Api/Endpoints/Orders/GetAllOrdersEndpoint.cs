using Dima.Api.Common.Api;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CoreConfiguration = Dima.Core.Configuration;
using Dima.Api.Services;

namespace Dima.Api.Endpoints.Orders
{
    public class GetAllOrdersEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
            => app.MapGet("/", HandleAsync)
                    .WithName("Orders: Get All")
                    .WithSummary("Orders: Get All (Paged)")
                    .WithDescription("Orders: Get All (Paged)")
                    .WithOrder(3)
                    .Produces<PagedResponse<List<Order>?>>();
        private static async Task<IResult> HandleAsync(
            ClaimsPrincipal user,
            IOrderHandler handler,
            OrderExpirationService expirationService,
            [FromQuery] int pageSize = CoreConfiguration.DefaultPageSize,
            [FromQuery] int pageNumber = CoreConfiguration.DefaultPageNumber)
        {
            var userName = user.Identity!.Name ?? string.Empty;

            var expirationResult =
                await expirationService.ExpirePendingForUserAsync(userName);

            var request = new GetAllOrdersRequest
            {
                UserId = userName,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Consulta depois da tentativa de expiração,
            // para retornar o estado atualizado do banco.
            var result = await handler.GetAllAsync(request);

            if (!result.IsSuccess)
                return TypedResults.BadRequest(result);

            // A falha na verificação não impede consultar os pedidos.
            result.Message = expirationResult.IsSuccess
                ? string.Empty
                : "Não foi possível concluir a verificação do pedido pendente. " +
                  "O estado exibido é o último confirmado. " +
                  "Atualize a página para tentar novamente.";

            return TypedResults.Ok(result);
        }
    }
}