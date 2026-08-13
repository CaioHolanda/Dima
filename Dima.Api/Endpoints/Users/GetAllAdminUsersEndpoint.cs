using Dima.Api.Common.Api;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;
using CoreConfiguration = Dima.Core.Configuration;

namespace Dima.Api.Endpoints.Users;

public class GetAllAdminUsersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/", HandleAsync)
            .WithName("Admin Users: Get All")
            .WithSummary("Gets all users for administration")
            .WithOrder(1)
            .Produces<PagedResponse<List<AdminUserListItem>?>>(
                StatusCodes.Status200OK);

    private static async Task<IResult> HandleAsync(
        IAdminUserHandler handler,
        [FromQuery] int pageSize =
            CoreConfiguration.DefaultPageSize,
        [FromQuery] int pageNumber =
            CoreConfiguration.DefaultPageNumber)
    {
        var request = new GetAllAdminUsersRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await handler.GetAllAsync(request);

        return result.ToResult();
    }
}