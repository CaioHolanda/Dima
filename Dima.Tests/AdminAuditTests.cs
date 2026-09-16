using System.Security.Claims;
using Dima.Api.Auditing;
using Dima.Api.Data;
using Dima.Api.Models;
using Dima.Core.Models;
using Dima.Core.Responses;
using Dima.Core.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dima.Tests;

public sealed class AdminAuditTests
{
    private static ServiceProvider Services()
    {
        var name = Guid.NewGuid().ToString();
        return new ServiceCollection().AddDbContext<AppDbContext>(x => x.UseInMemoryDatabase(name)).BuildServiceProvider();
    }

    private static DefaultHttpContext Http(string path, string method = "PUT", bool admin = true)
    {
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        http.Request.Method = method;
        http.Request.RouteValues["id"] = "1";
        http.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Name, "admin@example.com"),
            new Claim(ClaimTypes.Role, admin ? AppRoles.Admin : "User")
        }, "test"));
        return http;
    }

    [Fact]
    public async Task UpdateRecordsPersistedBeforeAndAfterValues()
    {
        using var services = Services();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Products.Add(new Product { Id = 1, Title = "Plan", Price = 100, AccessDurationMonths = 1 });
            await db.SaveChangesAsync();
        }
        var filter = new AdminAuditFilter(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AdminAuditFilter>.Instance);
        await filter.InvokeAsync(EndpointFilterInvocationContext.Create(Http("/api/v1/admin/products/1")), async _ =>
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Products.SingleAsync()).Price = 120;
            await db.SaveChangesAsync();
            return Results.Ok();
        });
        using var readScope = services.CreateScope();
        var log = await readScope.ServiceProvider.GetRequiredService<AppDbContext>().AdminAuditLogs.SingleAsync();
        Assert.Equal("42", log.ActorId);
        Assert.Equal(1L, log.TargetId);
        Assert.True(log.Succeeded);
        Assert.Equal(TimeSpan.Zero, log.OccurredAtUtc.Offset);
        Assert.Contains("\"Price\":100", log.BeforeJson);
        Assert.Contains("\"Price\":120", log.AfterJson);
    }

    [Fact]
    public async Task FailedUserActionRecordsResultWithoutSecrets()
    {
        using var services = Services();
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Users.Add(new User { Id = 1, Email = "user@example.com", PasswordHash = "secret-password", SecurityStamp = "secret-stamp" });
            await db.SaveChangesAsync();
        }
        var filter = new AdminAuditFilter(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AdminAuditFilter>.Instance);
        await filter.InvokeAsync(EndpointFilterInvocationContext.Create(Http("/api/v1/admin/users/1")), _ => ValueTask.FromResult<object?>(Results.Conflict()));
        using var readScope = services.CreateScope();
        var log = await readScope.ServiceProvider.GetRequiredService<AppDbContext>().AdminAuditLogs.SingleAsync();
        Assert.False(log.Succeeded);
        Assert.Equal(409, log.StatusCode);
        Assert.DoesNotContain("secret", log.BeforeJson);
        Assert.DoesNotContain("SecurityStamp", log.AfterJson);
    }

    [Theory]
    [InlineData("GET", true)]
    [InlineData("PUT", false)]
    public async Task ReadsAndNonAdministratorActionsAreNotAudited(string method, bool admin)
    {
        using var services = Services();
        var filter = new AdminAuditFilter(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AdminAuditFilter>.Instance);
        await filter.InvokeAsync(EndpointFilterInvocationContext.Create(Http("/api/v1/admin/products/1", method, admin)), _ => ValueTask.FromResult<object?>(Results.Ok()));
        using var scope = services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().AdminAuditLogs.ToListAsync());
    }

    [Fact]
    public async Task CreateRecordsGeneratedIdAndRefundExceptionIsAudited()
    {
        using var services = Services();
        var filter = new AdminAuditFilter(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<AdminAuditFilter>.Instance);
        var http = Http("/api/v1/admin/products", "POST");
        http.Request.RouteValues.Remove("id");
        await filter.InvokeAsync(EndpointFilterInvocationContext.Create(http), async _ =>
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var product = new Product { Title = "Plan", AccessDurationMonths = 1 };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return Results.Json(new Response<Product>(product, 201), statusCode: 201);
        });
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(EndpointFilterInvocationContext.Create(Http("/api/v1/orders/1/refund", "POST")),
                _ => throw new InvalidOperationException("gateway failure")));
        using var readScope = services.CreateScope();
        var logs = await readScope.ServiceProvider.GetRequiredService<AppDbContext>().AdminAuditLogs.OrderBy(x => x.Id).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.NotNull(logs[0].TargetId);
        Assert.Null(logs[0].BeforeJson);
        Assert.NotNull(logs[0].AfterJson);
        Assert.Equal(201, logs[0].StatusCode);
        Assert.Equal("Order", logs[1].TargetType);
        Assert.Equal(500, logs[1].StatusCode);
        Assert.False(logs[1].Succeeded);
    }
}
