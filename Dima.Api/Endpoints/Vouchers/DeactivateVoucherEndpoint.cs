using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class DeactivateVoucherEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPatch("/{id:long}/deactivate", HandleAsync)
            .WithName("Vouchers: Deactivate")
            .WithSummary("Deactivate a voucher")
            .WithDescription("Deactivates an existing voucher")
            .WithOrder(5)
            .Produces<Response<Voucher?>>(StatusCodes.Status200OK)
            .Produces<Response<Voucher?>>(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        long id)
    {
        var request = new DeactivateVoucherRequest
        {
            Id = id
        };

        var result = await handler.DeactivateAsync(request);

        return result.Code switch
        {
            StatusCodes.Status200OK =>
                TypedResults.Ok(result),

            StatusCodes.Status404NotFound =>
                TypedResults.NotFound(result),

            _ =>
                Results.Json(
                    result,
                    statusCode: result.Code)
        };
    }
}