using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class GetAdminVoucherByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{id:long}", HandleAsync)
            .WithName("Vouchers: Get By Id Admin")
            .WithSummary("Get voucher by id")
            .WithDescription("Gets a voucher by id for administration")
            .WithOrder(3)
            .Produces<Response<AdminVoucherDetails?>>(StatusCodes.Status200OK)
            .Produces<Response<AdminVoucherDetails?>>(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        long id)
    {
        var request = new GetVoucherByIdRequest
        {
            Id = id
        };

        var result = await handler.GetByIdForAdminAsync(request);

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