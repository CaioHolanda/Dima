using Dima.Api.Common.Api;
using Dima.Api.Models;
using Dima.Core.Requests.Account;
using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Endpoints.Identity;

public class LoginEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapPost("/login-user", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Login")
            .WithSummary("Authenticates a user");

    private static async Task<IResult> HandleAsync(
        LoginRequest request,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Results.Unauthorized();

        if (!await userManager.IsEmailConfirmedAsync(user))
        {
            return Results.Json(
                new { error = "NotAllowed" },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await signInManager.PasswordSignInAsync(
            user,
            request.Password,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
            return Results.Unauthorized();

        return Results.Ok();
    }
}