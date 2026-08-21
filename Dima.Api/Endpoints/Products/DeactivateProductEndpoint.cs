using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Products;

public class DeactivateProductEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapDelete("/{id:long}", HandleAsync)
            .WithName("Products: Deactivate")
            .WithSummary("Deactivate a product")
            .WithDescription("Logically deactivates an existing product")
            .WithOrder(3)
            .Produces<Response<Product?>>(StatusCodes.Status200OK)
            .Produces<Response<Product?>>(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminProductHandler handler,
        long id)
    {
        var request = new DeactivateProductRequest
        {
            Id = id
        };

        var result = await handler.DeactivateAsync(request);

        return result.ToResult();
    }
}