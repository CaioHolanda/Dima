using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Vouchers;

public class CreateVoucherEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/", HandleAsync)
            .WithName("Vouchers: Create")
            .WithSummary("Create a new voucher")
            .WithDescription("Creates a new voucher")
            .WithOrder(1)
            .Produces<Response<Voucher?>>(StatusCodes.Status201Created)
            .Produces<Response<Voucher?>>(StatusCodes.Status400BadRequest)
            .Produces<Response<Voucher?>>(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        IAdminVoucherHandler handler,
        CreateVoucherRequest request)
    {
        var result = await handler.CreateAsync(request);

        return result.Code switch
        {
            StatusCodes.Status201Created =>
                TypedResults.Created(
                    $"/v1/admin/vouchers/{result.Data?.Id}",
                    result),

            StatusCodes.Status400BadRequest =>
                TypedResults.BadRequest(result),

            StatusCodes.Status409Conflict =>
                TypedResults.Conflict(result),

            _ =>
                Results.Json(
                    result,
                    statusCode: result.Code)
        };
    }
}