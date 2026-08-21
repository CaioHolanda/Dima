using Dima.Api.Common.Api;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;

namespace Dima.Api.Endpoints.Users;

public class ActivateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app.MapPatch("/{id:long}/activate", HandleAsync)
            .WithName("Users: Activate")
            .WithSummary("Activate a user")
            .WithDescription("Reactivates an existing user")
            .WithOrder(3)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status200OK)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status400BadRequest)
            .Produces<Response<AdminUserListItem?>>(
                StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        IAdminUserHandler handler,
        long id)
    {
        var request = new ActivateUserRequest
        {
            Id = id
        };

        var result = await handler.ActivateAsync(request);

        return result.ToResult();
    }
}