using Dima.Api.Configuration;
using Dima.Api.Models;
using Dima.Core.Security;
using Microsoft.AspNetCore.Identity;
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

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("IdentitySeeder");

        await CreateRolesAsync(roleManager);

        await PromoteInitialAdminAsync(
            userManager,
            adminOptions,
            logger);
    }

    private static async Task CreateRolesAsync(
        RoleManager<IdentityRole<long>> roleManager)
    {
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
    }

    private static async Task PromoteInitialAdminAsync(
        UserManager<User> userManager,
        InitialAdminOptions adminOptions,
        ILogger logger)
    {
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
        {
            logger.LogInformation(
                "User '{Email}' is already an administrator.",
                adminOptions.Email);

            return;
        }

        var result = await userManager.AddToRoleAsync(
            user,
            AppRoles.Admin);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"[E113] Could not promote '{adminOptions.Email}' to Admin: {errors}");
        }

        logger.LogInformation(
            "User '{Email}' promoted to administrator.",
            adminOptions.Email);
    }
}