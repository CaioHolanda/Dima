using Dima.Core.Security;
using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Data.Seed;

public static class IdentitySeeder
{
    public static async Task SeedRolesAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<long>>>();

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
}