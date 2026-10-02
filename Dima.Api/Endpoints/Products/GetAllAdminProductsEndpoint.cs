using Dima.Api.Common.Api;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using CoreConfiguration = Dima.Core.Configuration;

namespace Dima.Api.Endpoints.Products;

public class GetAllAdminProductsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/", HandleAsync)
            .WithName("Admin Products: Get All")
            .WithSummary("Gets all products for administration")
            .WithOrder(4)
            .Produces<PagedResponse<List<Product>?>>(
                StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        IAdminProductHandler handler,
        [AsParameters] GetAllAdminProductsRequest request)
    {

        var result =
            await handler.GetAllForAdminAsync(request);

        return result.ToResult();
    }
}