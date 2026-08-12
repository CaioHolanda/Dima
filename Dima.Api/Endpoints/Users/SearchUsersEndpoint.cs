using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Dima.Api.Endpoints.Users;

public class SearchUsersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapGet("/lookup", HandleAsync)
            .WithName("Users: Search")
            .WithSummary("Search users by email")
            .WithDescription(
                "Returns a limited list of users matching the informed email")
            .WithOrder(1)
            .Produces<Response<List<UserLookup>?>>(
                StatusCodes.Status200OK)
            .Produces<Response<List<UserLookup>?>>(
                StatusCodes.Status400BadRequest);

    private static async Task<IResult> HandleAsync(
        IAdminUserHandler handler,
        [FromQuery] string searchTerm,
        [FromQuery] int limit = 10)
    {
        var request = new SearchUsersRequest
        {
            SearchTerm = searchTerm,
            Limit = limit
        };

        var result = await handler.SearchAsync(request);

        return result.IsSuccess
            ? TypedResults.Ok(result)
            : TypedResults.BadRequest(result);
    }
}