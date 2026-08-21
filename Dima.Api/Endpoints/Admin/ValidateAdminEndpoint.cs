using Dima.Api.Common.Api;
using Dima.Core.Security;
using System.Security.Claims;

namespace Dima.Api.Endpoints.Admin;

public class ValidateAdminEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapGet("/validate", Handle)
            .RequireAuthorization(AppPolicies.AdminOnly);

    private static IResult Handle(ClaimsPrincipal user)
    {
        return Results.Ok(new
        {
            message = "AdminOnly policy validated.",
            user = user.Identity?.Name
        });
    }
}