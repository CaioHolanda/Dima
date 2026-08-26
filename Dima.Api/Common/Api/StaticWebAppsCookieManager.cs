using Microsoft.AspNetCore.Authentication.Cookies;

namespace Dima.Api.Common.Api;

public sealed class StaticWebAppsCookieManager : ICookieManager
{
    private readonly ChunkingCookieManager _inner = new();

    public string? GetRequestCookie(
        HttpContext context,
        string key)
        => _inner.GetRequestCookie(context, key);

    public void AppendResponseCookie(
        HttpContext context,
        string key,
        string value,
        CookieOptions options)
        => _inner.AppendResponseCookie(
            context,
            key,
            value,
            options);

    public void DeleteCookie(
        HttpContext context,
        string key,
        CookieOptions options)
    {
        var deleteOptions = new CookieOptions
        {
            Domain = options.Domain,
            Path = options.Path,
            HttpOnly = options.HttpOnly,
            Secure = options.Secure,
            SameSite = options.SameSite,
            IsEssential = options.IsEssential,

            MaxAge = TimeSpan.Zero
        };

        context.Response.Cookies.Append(
            key,
            string.Empty,
            deleteOptions);
    }
}