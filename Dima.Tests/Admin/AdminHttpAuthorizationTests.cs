using System.Net;
using System.Net.Http.Json;
using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Api.Endpoints;
using Dima.Api.Models;
using Dima.Core.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dima.Tests.Admin;

public class AdminHttpAuthorizationTests
{
    public static IEnumerable<object[]> AccessCases()
    {
        foreach (var path in new[] { "products", "vouchers", "users", "orders", "validate" })
        foreach (var identity in new[] { "anonymous", "customer", "admin" })
            yield return [path, identity];
    }

    [Theory]
    [MemberData(nameof(AccessCases))]
    public async Task Admin_routes_enforce_authorization_over_http(string path, string identity)
    {
        await using var app = await CreateAppAsync();
        using var client = await CreateClientAsync(app, identity);
        using var response = await client.GetAsync($"/api/v1/admin/{path}?pageNumber=1&pageSize=25");
        var expected = identity == "anonymous" ? HttpStatusCode.Unauthorized
            : identity == "customer" ? HttpStatusCode.Forbidden : HttpStatusCode.OK;
        Assert.Equal(expected, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Theory]
    [InlineData("anonymous", HttpStatusCode.Unauthorized)]
    [InlineData("customer", HttpStatusCode.Forbidden)]
    [InlineData("admin", HttpStatusCode.Created)]
    public async Task Product_creation_requires_admin_and_rejected_requests_do_not_write(
        string identity, HttpStatusCode expected)
    {
        await using var app = await CreateAppAsync();
        using var client = await CreateClientAsync(app, identity);
        using var response = await client.PostAsJsonAsync("/api/v1/admin/products", new
        {
            Title = "Plano HTTP", Description = "Teste de autorização", Slug = "plano-http",
            Price = 100m, AccessDurationMonths = 1, IsActive = true
        });
        Assert.Equal(expected, response.StatusCode);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(identity == "admin" ? 1 : 0, await db.Products.CountAsync());
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        // Real routes, cookie authentication and authorization; isolated database and loopback HTTP.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Development", ContentRootPath = AppContext.BaseDirectory
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        var database = Guid.NewGuid().ToString();
        builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(database));
        builder.Services.AddIdentityCore<User>(options => options.SignIn.RequireConfirmedEmail = true)
            .AddRoles<IdentityRole<long>>().AddEntityFrameworkStores<AppDbContext>().AddApiEndpoints();
        builder.AddSecurity();
        builder.AddServices();
        builder.AddEmailServices();
        var app = builder.Build();
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();
                Assert.True((await roles.CreateAsync(new IdentityRole<long>(AppRoles.Admin))).Succeeded);
                Assert.True((await roles.CreateAsync(new IdentityRole<long>(AppRoles.User))).Succeeded);
                var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                foreach (var identity in new[] { "customer", "admin" })
                {
                    var user = new User { UserName = $"{identity}@test.com", Email = $"{identity}@test.com", EmailConfirmed = true };
                    Assert.True((await users.CreateAsync(user, "Dt17-Test123!")).Succeeded);
                    Assert.True((await users.AddToRoleAsync(user, identity == "admin" ? AppRoles.Admin : AppRoles.User)).Succeeded);
                }
            }
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapEndpoints();
            await app.StartAsync();
            return app;
        }
        catch
        {
            await app.DisposeAsync();
            throw;
        }
    }

    private static async Task<HttpClient> CreateClientAsync(WebApplication app, string identity)
    {
        var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = true })
        {
            BaseAddress = new Uri(app.Urls.Single()), Timeout = TimeSpan.FromSeconds(15)
        };
        try
        {
            if (identity != "anonymous")
            {
                using var login = await client.PostAsJsonAsync("/api/v1/identity/login-user", new
                {
                    Email = $"{identity}@test.com", Password = "Dt17-Test123!"
                });
                Assert.Equal(HttpStatusCode.OK, login.StatusCode);
                Assert.True(login.Headers.Contains("Set-Cookie"));
            }
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}

