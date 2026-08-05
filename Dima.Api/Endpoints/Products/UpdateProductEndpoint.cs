using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Products;

public class UpdateProductEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPut("/{id:long}", HandleAsync)
            .WithName("Products: Update")
            .WithSummary("Update a product")
            .WithDescription("Updates an existing product")
            .WithOrder(2)
            .Produces<Response<Product?>>(StatusCodes.Status200OK)
            .Produces<Response<Product?>>(StatusCodes.Status400BadRequest)
            .Produces<Response<Product?>>(StatusCodes.Status404NotFound)
            .Produces<Response<Product?>>(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        IProductHandler handler,
        UpdateProductRequest request,
        long id)
    {
        request.Id = id;

        var result = await handler.UpdateAsync(request);

        return result.ToResult();
    }
}