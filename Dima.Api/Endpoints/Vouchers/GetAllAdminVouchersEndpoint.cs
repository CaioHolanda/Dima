using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class GetAllAdminVouchersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/", HandleAsync)
            .WithName("Vouchers: Get All Admin")
            .WithSummary("Get all vouchers")
            .WithDescription("Gets all vouchers for administration")
            .WithOrder(2)
            .Produces<PagedResponse<List<Voucher>?>>();

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        [AsParameters] GetAllAdminVouchersRequest request)
    {
        var result = await handler.GetAllForAdminAsync(request);

        return result.IsSuccess
            ? TypedResults.Ok(result)
            : Results.Json(
                result,
                statusCode: result.Code);
    }
}