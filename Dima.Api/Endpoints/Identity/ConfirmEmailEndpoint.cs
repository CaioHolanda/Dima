using Dima.Api.Common.Api;
using Dima.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace Dima.Api.Endpoints.Identity;

public class ConfirmEmailEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
        => app
            .MapGet("/confirm-email", HandleAsync)
            .AllowAnonymous()
            .WithName("Identity: Confirm Email")
            .WithSummary("Confirms a user's email");

    private static async Task<IResult> HandleAsync(
        string userId,
        string code,
        UserManager<User> userManager)
    {
        if (!long.TryParse(userId, out var id))
        {
            return Results.BadRequest(
                "[E208] Usuario invalido");
        }

        var user = await userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return Results.BadRequest(
                "[E210] Usuario invalido");
        }

        string token;

        try
        {
            token = Encoding.UTF8.GetString(
                WebEncoders.Base64UrlDecode(code));
        }
        catch (FormatException)
        {
            return Results.BadRequest(
                "[E209] Token de confirmacao invalido");
        }

        var result = await userManager.ConfirmEmailAsync(
            user,
            token);

        if (!result.Succeeded)
        {
            return Results.BadRequest(
                "[E211] Token de confirmacao invalido");
        }

        return Results.Ok();
    }
}