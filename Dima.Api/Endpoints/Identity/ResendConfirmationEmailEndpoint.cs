using Dima.Api.Common.Api;
using Dima.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Dima.Api.Endpoints.Identity;

public class ResendConfirmationEmailEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapPost("/resend-confirmation-email", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Resend Confirmation Email")
            .WithSummary("Resends the email confirmation link");

    private static async Task<IResult> HandleAsync(
        ResendConfirmationEmailRequest request,
        UserManager<User> userManager,
        IEmailSender<User> emailSender,
        HttpContext httpContext)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null)
            return Results.Ok();

        if (await userManager.IsEmailConfirmedAsync(user))
            return Results.Ok();

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
            user.Email!,
            confirmationLink);

        return Results.Ok();
    }

    public sealed record ResendConfirmationEmailRequest(
        string Email);
}