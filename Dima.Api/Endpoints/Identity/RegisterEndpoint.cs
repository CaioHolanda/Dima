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
        LinkGenerator linkGenerator,
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
            return Results.ValidationProblem(
                createResult.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.Description)
                            .ToArray()));
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            AppRoles.User);

        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);

            return Results.ValidationProblem(
                roleResult.Errors
                    .GroupBy(error => error.Code)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(error => error.Description)
                            .ToArray()));
        }

        var confirmationToken =
            await userManager.GenerateEmailConfirmationTokenAsync(user);

        var encodedToken = WebEncoders.Base64UrlEncode(
            Encoding.UTF8.GetBytes(confirmationToken));

        var confirmationLink = linkGenerator.GetUriByName(
            httpContext,
            "Identity: ConfirmEmail",
            new
            {
                userId = user.Id,
                code = encodedToken
            });

        if (string.IsNullOrWhiteSpace(confirmationLink))
        {
            throw new InvalidOperationException(
                "[E110] Não foi possível gerar o link de confirmação de e-mail.");
        }

        await emailSender.SendConfirmationLinkAsync(
            user,
            request.Email,
            confirmationLink);

        return Results.Ok();
    }
}