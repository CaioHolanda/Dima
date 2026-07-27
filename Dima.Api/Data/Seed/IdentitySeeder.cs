using Dima.Core.Security;
using Microsoft.AspNetCore.Identity;
using Dima.Api.Configuration;
using Dima.Api.Models;
using Microsoft.Extensions.Options;

namespace Dima.Api.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<long>>>();
 
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<User>>();

        var adminOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<InitialAdminOptions>>()
            .Value;

        foreach (var roleName in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                continue;

            var role = new IdentityRole<long>(roleName);

            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
                continue;

            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"[E110] Não foi possível criar a role '{roleName}': {errors}");
        }
        if (!adminOptions.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(adminOptions.Email))
            throw new InvalidOperationException(
                "[E111] InitialAdmin:Email was not configured.");

        var user = await userManager.FindByEmailAsync(adminOptions.Email);

        if (user is null)
            throw new InvalidOperationException(
                $"[E112] User '{adminOptions.Email}' was not found.");

        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
            return;

        var promoteResult =
        await userManager.AddToRoleAsync(user, AppRoles.Admin);

        if (!promoteResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                promoteResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"[E113] Could not promote '{adminOptions.Email}' to Admin: {errors}");
        }
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeeder");

        logger.LogInformation(
            "User '{Email}' promoted to administrator.",
            adminOptions.Email);

    }
}