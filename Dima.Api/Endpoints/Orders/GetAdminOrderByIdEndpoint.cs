using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Orders;

public class GetAdminOrderByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{id:long}", HandleAsync)
            .WithName("Admin Orders: Get By Id")
            .WithSummary("Consulta os registros operacionais de um pedido")
            .Produces<Response<AdminOrderDetails?>>()
            .Produces<Response<AdminOrderDetails?>>(404)
            .Produces<Response<AdminOrderDetails?>>(500);

    private static async Task<IResult> HandleAsync(long id, IAdminOrderHandler handler)
        => (await handler.GetByIdAsync(id)).ToResult();
}