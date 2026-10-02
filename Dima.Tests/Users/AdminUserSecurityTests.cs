using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Core.Requests.Users;
using Dima.Core.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dima.Tests.Users;

public class AdminUserSecurityTests
{
    [Theory]
    [InlineData("self", false)]
    [InlineData("last", false)]
    [InlineData("other", true)]
    [InlineData("locked", false)]
    [InlineData("unconfirmed", false)]
    [InlineData("ordinary", true)]
    public async Task Deactivation_protects_administrators_and_revokes_old_credentials(string scenario, bool allowed)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();
        services.AddScoped<AppDbContext>(_ => new TestContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options));
        services.AddIdentityCore<User>().AddRoles<IdentityRole<long>>()
            .AddEntityFrameworkStores<AppDbContext>().AddSignInManager();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.EnsureCreatedAsync();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<long>>>();
        Assert.True((await roles.CreateAsync(new IdentityRole<long>(AppRoles.Admin))).Succeeded);
        var target = new User { UserName = "target@test.com", Email = "target@test.com", EmailConfirmed = true };
        Assert.True((await manager.CreateAsync(target)).Succeeded);
        if (scenario != "ordinary")
            Assert.True((await manager.AddToRoleAsync(target, AppRoles.Admin)).Succeeded);
        var actor = new User { UserName = "actor@test.com", Email = "actor@test.com", EmailConfirmed = scenario != "unconfirmed" };
        Assert.True((await manager.CreateAsync(actor)).Succeeded);
        if (scenario is "other" or "locked" or "unconfirmed" or "ordinary")
            Assert.True((await manager.AddToRoleAsync(actor, AppRoles.Admin)).Succeeded);
        if (scenario == "locked")
        {
            await manager.SetLockoutEnabledAsync(actor, true);
            await manager.SetLockoutEndDateAsync(actor, DateTimeOffset.UtcNow.AddMinutes(10));
        }
        var stamp = target.SecurityStamp;
        var signIn = scope.ServiceProvider.GetRequiredService<SignInManager<User>>();
        var oldPrincipal = await signIn.CreateUserPrincipalAsync(target);
        var handler = new AdminUserHandler(context, manager, TimeProvider.System);
        var result = await handler.DeactivateAsync(new DeactivateUserRequest
        {
            Id = target.Id, ActorId = scenario == "self" ? target.Id : actor.Id
        });
        Assert.Equal(allowed, result.IsSuccess);
        if (allowed)
        {
            Assert.True(await manager.IsLockedOutAsync(target));
            Assert.NotEqual(stamp, target.SecurityStamp);
            Assert.Null(await signIn.ValidateSecurityStampAsync(oldPrincipal));
            Assert.True((await handler.ActivateAsync(new ActivateUserRequest { Id = target.Id })).IsSuccess);
            Assert.False(await manager.IsLockedOutAsync(target));
            Assert.Null(await signIn.ValidateSecurityStampAsync(oldPrincipal));
        }
        else
        {
            Assert.Equal(stamp, target.SecurityStamp);
            Assert.NotEqual(DateTimeOffset.MaxValue, target.LockoutEnd);
        }
    }

    [Fact]
    public void Application_cookie_validates_stamp_on_every_request()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddSecurity();
        using var provider = builder.Services.BuildServiceProvider();
        Assert.Equal(TimeSpan.Zero, provider.GetRequiredService<IOptions<SecurityStampValidatorOptions>>().Value.ValidationInterval);
        var cookie = provider.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        Assert.NotNull(cookie.Events.OnValidatePrincipal);
    }

    private sealed class TestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToList())
                    entity.RemoveCheckConstraint(constraint.Name!);
            // SQLite lacks native DateTimeOffset ordering; SQL Server supports it.
            modelBuilder.Entity<User>().Property(x => x.LockoutEnd)
                .HasConversion(x => x.HasValue ? x.Value.UtcTicks : (long?)null,
                    x => x.HasValue ? new DateTimeOffset(x.Value, TimeSpan.Zero) : null);
        }
    }
}
