using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Vouchers;

public class ApplyVoucherEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/apply", HandleAsync)
            .WithName("Vouchers: Apply")
            .WithSummary("Valida e aplica um voucher")
            .WithDescription(
                "Valida o voucher para o usuário e produto informados")
            .WithOrder(2)
            .Produces<Response<VoucherApplication?>>(
                StatusCodes.Status200OK)
            .Produces<Response<VoucherApplication?>>(
                StatusCodes.Status400BadRequest)
            .Produces<Response<VoucherApplication?>>(
                StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IVoucherHandler handler,
        ApplyVoucherRequest request,
        ClaimsPrincipal user)
    {
        request.UserId =
            user.Identity?.Name ?? string.Empty;

        var result = await handler.ApplyAsync(request);

        return result.Code switch
        {
            StatusCodes.Status200OK =>
                TypedResults.Ok(result),

            StatusCodes.Status400BadRequest =>
                TypedResults.BadRequest(result),

            StatusCodes.Status404NotFound =>
                TypedResults.NotFound(result),

            _ =>
                Results.Json(
                    result,
                    statusCode: result.Code)
        };
    }
}