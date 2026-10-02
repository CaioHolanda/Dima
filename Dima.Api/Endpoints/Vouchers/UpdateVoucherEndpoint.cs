using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class UpdateVoucherEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPut("/{id:long}", HandleAsync)
            .WithName("Vouchers: Update")
            .WithSummary("Update a voucher")
            .WithDescription("Updates an existing voucher")
            .WithOrder(4)
            .Produces<Response<Voucher?>>(StatusCodes.Status200OK)
            .Produces<Response<Voucher?>>(StatusCodes.Status400BadRequest)
            .Produces<Response<Voucher?>>(StatusCodes.Status404NotFound)
            .Produces<Response<Voucher?>>(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        long id,
        UpdateVoucherRequest request)
    {
        request.Id = id;

        var result = await handler.UpdateAsync(request);

        return result.Code switch
        {
            StatusCodes.Status200OK =>
                TypedResults.Ok(result),

            StatusCodes.Status400BadRequest =>
                TypedResults.BadRequest(result),

            StatusCodes.Status404NotFound =>
                TypedResults.NotFound(result),

            StatusCodes.Status409Conflict =>
                TypedResults.Conflict(result),

            _ =>
                Results.Json(
                    result,
                    statusCode: result.Code)
        };
    }
}