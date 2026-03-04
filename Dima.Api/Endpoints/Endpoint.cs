using Dima.Api.Common.Api;
using Dima.Api.Endpoints.Categories;
using Dima.Core.Requests.Categories;
namespace Dima.Api.Endpoints;
public static class Endpoint
{
    // Extension Method
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoint = app.MapGroup("");
        endpoint.MapGroup("v1/categories")
            .WithTags("categories")
            .MapEndpoint<CreateCategoryEndpoint>()
            .MapEndpoint<UpdateCategoryEndpoint>()
            .MapEndpoint<DeleteCategoryEndpoint>()
            .MapEndpoint<GetCategoryByIdEndpoint>()
            .MapEndpoint<GetAllCategoriesEndpoint>();
    }
    private static IEndpointRouteBuilder MapEndpoint<TEndpoint> (this IEndpointRouteBuilder app)
        where TEndpoint:IEndpoint
    {
        TEndpoint.Map(app);
            return app;
    }
}

