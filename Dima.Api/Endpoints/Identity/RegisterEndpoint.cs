using Dima.Api.Common.Api;
using Dima.Api.Models;
using Dima.Core.Requests.Account;
using Dima.Core.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Dima.Api.Endpoints.Identity;

public class RegisterEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/register-user", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Register")
            .WithSummary("Registers a new user")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> HandleAsync(
        RegisterRequest request,
        UserManager<User> userManager,
        IEmailSender<User> emailSender,
        HttpContext httpContext)
    {
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email
        };

        var createResult = await userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            return CreateValidationProblem(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            AppRoles.User);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            return CreateValidationProblem(roleResult);
        }

        var confirmationToken =
            await userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(confirmationToken));

        var confirmationLink = QueryHelpers.AddQueryString(
            $"{httpContext.Request.Scheme}://" +
            $"{httpContext.Request.Host}" +
            $"{httpContext.Request.PathBase}" +
            "/api/v1/identity/confirm-email",
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id.ToString(),
                ["code"] = encodedToken
            });

        await emailSender.SendConfirmationLinkAsync(
            user,
            request.Email,
            confirmationLink);

        return Results.Ok();
    }

    private static IResult CreateValidationProblem(
        IdentityResult result)
    {
        var errors = result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(error => error.Description)
                    .ToArray());

        return Results.ValidationProblem(errors);
    }
}