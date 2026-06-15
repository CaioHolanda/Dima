using Dima.Api.Common.Api;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Orders
{
    public class GetAllOrdersEndpoint : IEndpoint
    {
        public static void Map(IEndpointRouteBuilder app)
            => app.MapGet("/", HandleAsync)
                    .WithName("Orders: Get All")
                    .WithSummary("Orders: Get All (Paged)")
                    .WithDescription("Orders: Get All (Paged)")
                    .WithOrder(3)
                    .Produces<PagedResponse<List<Order>?>>();
        private static async Task<IResult> HandleAsync(
            ClaimsPrincipal user,
            IOrderHandler handler,
            [FromQuery] int pageSize = Configuration.DefaultPageSize,          
            [FromQuery] int pageNumber = Configuration.DefaultPageNumber)
        {
            var request = new GetAllOrdersRequest
            {
                UserId = user.Identity!.Name ?? string.Empty,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await handler.GetAllAsync(request);
            return result.IsSuccess
                ? TypedResults.Ok(result)
                : TypedResults.BadRequest(result);
        }
    }
}