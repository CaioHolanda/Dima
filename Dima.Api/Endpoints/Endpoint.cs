using Dima.Api.Common.Api;
using Dima.Api.Endpoints.Categories;
using Dima.Api.Endpoints.Transactions;
using Dima.Core.Requests.Categories;
namespace Dima.Api.Endpoints;
public static class Endpoint
{
    // Extension Method
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoint = app
            .MapGroup("");

        endpoint.MapGroup("v1/categories")
            .WithTags("Categories")
            .RequireAuthorization()
            .MapEndpoint<CreateCategoryEndpoint>()
            .MapEndpoint<UpdateCategoryEndpoint>()
            .MapEndpoint<DeleteCategoryEndpoint>()
            .MapEndpoint<GetCategoryByIdEndpoint>()
            .MapEndpoint<GetAllCategoriesEndpoint>();

        endpoint.MapGroup("v1/transactions")
            .WithTags("Transactions")
            .RequireAuthorization()
            .MapEndpoint<CreateTransactionEndpoint>()
            .MapEndpoint<UpdateTransactionEndpoint>()
            .MapEndpoint<DeleteTransactionEndpoint>()
            .MapEndpoint<GetTransactionByIdEndpoint>()
            .MapEndpoint<GetTransactionsByPeriodEndpoint>();
    }
    private static IEndpointRouteBuilder MapEndpoint<TEndpoint> (this IEndpointRouteBuilder app)
        where TEndpoint:IEndpoint
    {
        TEndpoint.Map(app);
            return app;
    }
}

