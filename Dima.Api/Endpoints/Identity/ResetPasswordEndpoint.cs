using Dima.Api.Common.Api;
using Dima.Api.Models;
using Dima.Core.Requests.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Dima.Api.Endpoints.Identity;

public class ResetPasswordEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapPost("/reset-password", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Reset Password")
            .WithSummary("Resets a user's password");

    private static async Task<IResult> HandleAsync(
        ResetPasswordRequest request,
        UserManager<User> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null ||
            !await userManager.IsEmailConfirmedAsync(user))
        {
            return Results.BadRequest(
                "[E207] Token de redefinicao invalido");
        }

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(request.ResetCode));
        }
        catch (FormatException)
        {
            return Results.BadRequest(
                "[E207] Token de redefinicao invalido");
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            token,
            request.NewPassword);

        if (!result.Succeeded)
        {
            return Results.ValidationProblem(
                result.Errors
                    .GroupBy(x => x.Code)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Select(e => e.Description).ToArray()));
        }

        return Results.Ok();
    }
}