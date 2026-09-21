using Dima.Api.Services;
using Dima.Api.Common.Api;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Endpoints.Identity;

public sealed class SessionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/session", (HttpContext context, UserSessionService sessions) => HandleAsync(context, sessions, false))
            .RequireAuthorization();
        app.MapPost("/session/activity", (HttpContext context, UserSessionService sessions) => HandleAsync(context, sessions, true))
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(HttpContext context, UserSessionService sessions, bool activity)
    {
        context.Response.Headers.CacheControl = "no-store";
        var auth = await context.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!Guid.TryParse(auth.Properties?.GetString(UserSessionService.CookieKey), out var id))
            return Results.Unauthorized();
        // A custom header prevents cross-origin form submissions from extending the session.
        if (activity && context.Request.Headers["X-Requested-With"] != "XMLHttpRequest")
            return Results.BadRequest();
        if (activity && context.Request.Headers["X-Dima-Session"] != id.ToString())
            return Results.Conflict();
        if (activity && !await sessions.TouchAsync(id)) return Results.Unauthorized();
        var expiry = await sessions.GetExpiryAsync(id);
        return expiry is null ? Results.Unauthorized() : Results.Ok(new { sessionId = id, expiresUtc = expiry });
    }
}
