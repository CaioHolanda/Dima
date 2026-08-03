using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Products;

public class GetAdminProductByIdEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{id:long}", HandleAsync)
            .WithName("Admin Products: Get By Id")
            .WithSummary("Gets a product by ID for administration")
            .WithOrder(5)
            .Produces<Response<Product?>>(
                StatusCodes.Status200OK)
            .Produces<Response<Product?>>(
                StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IProductHandler handler,
        long id)
    {
        var request = new GetProductByIdRequest
        {
            Id = id
        };

        var result =
            await handler.GetByIdForAdminAsync(request);

        return result.ToResult();
    }
}