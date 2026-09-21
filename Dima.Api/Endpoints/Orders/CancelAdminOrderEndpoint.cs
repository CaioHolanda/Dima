using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Responses;
using Dima.Core.Security;

namespace Dima.Api.Endpoints.Orders;

public class CancelAdminOrderEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/{id:long}/cancel", HandleAsync)
            .RequireAuthorization(AppPolicies.AdminOnly)
            .WithName("Admin Orders: Cancel")
            .WithSummary("Cancela um pedido válido aguardando pagamento")
            .Produces<Response<bool>>()
            .Produces<Response<bool>>(404)
            .Produces<Response<bool>>(409)
            .Produces<Response<bool>>(500)
            .Produces<Response<bool>>(502);

    private static async Task<IResult> HandleAsync(long id, IAdminOrderHandler handler)
        => (await handler.CancelAsync(id)).ToResult();
}
