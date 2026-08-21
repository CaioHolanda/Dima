using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Products;

public class ActivateProductEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPut("/{id:long}/activate", HandleAsync)
            .WithName("Products: Activate")
            .WithSummary("Activate a product")
            .WithDescription("Reactivates an existing product")
            .WithOrder(4)
            .Produces<Response<Product?>>(
                StatusCodes.Status200OK)
            .Produces<Response<Product?>>(
                StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminProductHandler handler,
        long id)
    {
        var request = new ActivateProductRequest
        {
            Id = id
        };

        var result = await handler.ActivateAsync(request);

        return result.ToResult();
    }
}