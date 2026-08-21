using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class ActivateVoucherEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPatch("/{id:long}/activate", HandleAsync)
            .WithName("Vouchers: Activate")
            .WithSummary("Activate a voucher")
            .WithDescription("Activates an existing voucher")
            .WithOrder(6)
            .Produces<Response<Voucher?>>(StatusCodes.Status200OK)
            .Produces<Response<Voucher?>>(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        long id)
    {
        var request = new ActivateVoucherRequest
        {
            Id = id
        };

        var result = await handler.ActivateAsync(request);

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