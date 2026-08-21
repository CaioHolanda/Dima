using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using CoreConfiguration = Dima.Core.Configuration;

namespace Dima.Api.Endpoints.Orders;

public class GetAllAdminOrdersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/", HandleAsync)
            .WithName("Admin Orders: Get All")
            .WithSummary("List all orders")
            .WithDescription(
                "Returns a paged list of all orders for administrative viewing")
            .WithOrder(1)
            .Produces<PagedResponse<List<AdminOrderListItem>?>>(
                StatusCodes.Status200OK)
            .Produces<PagedResponse<List<AdminOrderListItem>?>>(
                StatusCodes.Status400BadRequest)
            .Produces<PagedResponse<List<AdminOrderListItem>?>>(
                StatusCodes.Status401Unauthorized)
            .Produces<PagedResponse<List<AdminOrderListItem>?>>(
                StatusCodes.Status403Forbidden)
            .Produces<PagedResponse<List<AdminOrderListItem>?>>(
                StatusCodes.Status500InternalServerError);

    private static async Task<IResult> HandleAsync(
        IAdminOrderHandler handler,
        [FromQuery] int pageSize =
            CoreConfiguration.DefaultPageSize,
        [FromQuery] int pageNumber =
            CoreConfiguration.DefaultPageNumber)
    {
        var request = new GetAllAdminOrdersRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await handler.GetAllAsync(request);

        return result.ToResult();
    }
}