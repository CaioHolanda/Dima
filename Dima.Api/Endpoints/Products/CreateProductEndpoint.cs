using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Products;

public class CreateProductEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPost("/", HandleAsync)
            .WithName("Products: Create")
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product")
            .WithOrder(1)
            .Produces<Response<Product?>>(StatusCodes.Status201Created)
            .Produces<Response<Product?>>(StatusCodes.Status400BadRequest);

    private static async Task<IResult> HandleAsync(
        IAdminProductHandler handler,
        CreateProductRequest request)
    {
        var result = await handler.CreateAsync(request);

        return result.Code switch
        {
            StatusCodes.Status201Created =>
                TypedResults.Created(
                    $"/v1/admin/products/{result.Data?.Id}",
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