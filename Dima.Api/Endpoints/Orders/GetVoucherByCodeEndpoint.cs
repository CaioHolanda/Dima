using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Orders;

public class GetVoucherByCodeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{code}", HandleAsync)
            .WithName("Voucher: By Code")
            .WithSummary("Recupera um voucher pelo código")
            .WithDescription("Recupera um voucher ativo pelo código")
            .WithOrder(1)
            .Produces<Response<Voucher?>>();

    private static async Task<IResult> HandleAsync(
        IVoucherHandler handler,
        string code)
    {
        var request = new GetVoucherByCodeRequest
        {
            Code = code
        };

        var result = await handler.GetByCodeAsync(request);

        return result.IsSuccess
            ? TypedResults.Ok(result)
            : TypedResults.NotFound(result);
    }
}