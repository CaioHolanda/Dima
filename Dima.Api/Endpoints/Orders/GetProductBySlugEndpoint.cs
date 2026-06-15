using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class GetProductBySlugEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/{slug}", HandleAsync)
            .WithName("Product: By slug")
            .WithSummary("Read one Product")
            .WithDescription("Read one Product")
            .WithOrder(2)
            .Produces<Response<Product?>>();

        private static async Task<IResult> HandleAsync(
            IProductHanlder handler,
            string slug)
        {
            var request = new GetProductBySlugRequest
            {
                Slug = slug
            };
            var result = await handler.GetBySlugAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}