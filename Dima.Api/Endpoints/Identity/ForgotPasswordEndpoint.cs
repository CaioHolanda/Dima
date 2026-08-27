using Dima.Api.Common.Api;
using Dima.Api.Models;
using Dima.Core.Requests.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Dima.Api.Endpoints.Identity;

public class ForgotPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapPost("/forgot-password", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Forgot Password")
            .WithSummary("Requests a password reset");

    private static async Task<IResult> HandleAsync(
        ForgotPasswordRequest request,
        UserManager<User> userManager,
        IEmailSender<User> emailSender,
        HttpContext httpContext)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Results.Ok();

        if (!await userManager.IsEmailConfirmedAsync(user))
            return Results.Ok();

        var resetToken =
            await userManager.GeneratePasswordResetTokenAsync(user);

        var encodedToken =
            WebEncoders.Base64UrlEncode(
                Encoding.UTF8.GetBytes(resetToken));

        var resetLink = QueryHelpers.AddQueryString(
            $"{Dima.Core.Configuration.FrontendUrl}/reset-password",
            new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["code"] = encodedToken
            });

        await emailSender.SendPasswordResetLinkAsync(
            user,
            user.Email!,
            resetLink);

        return Results.Ok();
    }
}