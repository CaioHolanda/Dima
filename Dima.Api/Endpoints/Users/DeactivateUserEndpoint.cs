using Dima.Api.Common.Api;
using System.Security.Claims;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Users;

public class DeactivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPatch("/{id:long}/deactivate", HandleAsync)
            .WithName("Users: Deactivate")
            .WithSummary("Deactivate a user")
            .WithDescription("Deactivates an existing user")
            .WithOrder(4)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status200OK)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status400BadRequest)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status403Forbidden)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminUserHandler handler,
        ClaimsPrincipal principal,
        long id)
    {
        if (!long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId))
            return Results.Unauthorized();

        var request = new DeactivateUserRequest
        {
            Id = id,
            ActorId = actorId
        };

        var result = await handler.DeactivateAsync(request);

        return result.ToResult();
    }
}