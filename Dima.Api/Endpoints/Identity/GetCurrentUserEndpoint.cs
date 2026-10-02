using Dima.Api.Common.Api;
using Dima.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Endpoints.Identity;

public class GetCurrentUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapGet("/me", HandleAsync)
            .WithName("Identity: Current User")
            .WithSummary("Returns the authenticated user");

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        UserManager<User> userManager)
    {
        var user =
            await userManager.GetUserAsync(httpContext.User);

        if (user is null)
            return Results.Unauthorized();

        var claims =
            await userManager.GetClaimsAsync(user);

        var response =
            new Dima.Core.Models.Account.User
            {
                Email = user.Email ?? string.Empty,
                IsMailConfirmed = user.EmailConfirmed,
                Claims = claims.ToDictionary(
                    x => x.Type,
                    x => x.Value)
            };

        return Results.Ok(response);
    }
}