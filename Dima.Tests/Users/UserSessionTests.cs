using System.Net;
using System.Net.Http.Json;
using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Api.Endpoints.Identity;
using Dima.Api.Models;
using Dima.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dima.Tests.Users;

public sealed class UserSessionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Idle_expiration_rejects_reads_and_cannot_be_revived(bool activity)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.GetAsync("/api/v1/identity/session")).StatusCode);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        var response = activity
            ? await fixture.Client.PostAsync("/api/v1/identity/session/activity", null)
            : await fixture.Client.GetAsync("/api/v1/identity/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task Human_activity_extends_idle_time_but_not_eight_hour_limit()
    {
        await using var fixture = await Fixture.CreateAsync();
        for (var minute = 10; minute < 480; minute += 10)
        {
            fixture.Clock.Advance(TimeSpan.FromMinutes(10));
            Assert.Equal(HttpStatusCode.OK,
                (await fixture.Client.PostAsync("/api/v1/identity/session/activity", null)).StatusCode);
        }
        fixture.Clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await fixture.Client.PostAsync("/api/v1/identity/session/activity", null)).StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_copied_cookie_and_stamp_changes_remain_effective()
    {
        await using var fixture = await Fixture.CreateAsync();
        var cookie = fixture.Cookie;
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.PostAsync("/api/v1/identity/logout", null)).StatusCode);
        using var replay = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = fixture.Client.BaseAddress };
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await replay.GetAsync("/protected")).StatusCode);

        await fixture.LoginAsync();
        using (var scope = fixture.App.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = (await manager.FindByEmailAsync("session@test.com"))!;
            Assert.True((await manager.UpdateSecurityStampAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task Old_tab_cannot_extend_another_session()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Clock.Advance(TimeSpan.FromMinutes(14));
        fixture.Client.DefaultRequestHeaders.Remove("X-Dima-Session");
        fixture.Client.DefaultRequestHeaders.Add("X-Dima-Session", Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Conflict,
            (await fixture.Client.PostAsync("/api/v1/identity/session/activity", null)).StatusCode);
        fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.Client.GetAsync("/protected")).StatusCode);
    }

    [Fact]
    public async Task Activity_requires_header_and_forbidden_response_does_not_end_session()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Client.DefaultRequestHeaders.Remove("X-Requested-With");
        Assert.Equal(HttpStatusCode.BadRequest,
            (await fixture.Client.PostAsync("/api/v1/identity/session/activity", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await fixture.Client.GetAsync("/admin")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await fixture.Client.GetAsync("/protected")).StatusCode);
    }

    private sealed class Clock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan elapsed) => _now += elapsed;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public WebApplication App { get; private set; } = null!;
        public HttpClient Client { get; private set; } = null!;
        public Clock Clock { get; } = new();
        public string Cookie { get; private set; } = "";
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public static async Task<Fixture> CreateAsync()
        {
            var fixture = new Fixture();
            await fixture._connection.OpenAsync();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
            builder.Services.AddScoped<AppDbContext>(_ => new TestContext(
                new DbContextOptionsBuilder<AppDbContext>().UseSqlite(fixture._connection).Options));
            builder.Services.AddIdentityCore<User>().AddRoles<IdentityRole<long>>()
                .AddEntityFrameworkStores<AppDbContext>().AddApiEndpoints();
            builder.AddSecurity();
            builder.Services.AddSingleton<TimeProvider>(fixture.Clock);
            fixture.App = builder.Build();
            using (var scope = fixture.App.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
                Assert.True((await manager.CreateAsync(new User { UserName = "session@test.com", Email = "session@test.com", EmailConfirmed = true }, "Session-Test123!")).Succeeded);
            }
            fixture.App.UseAuthentication();
            fixture.App.UseAuthorization();
            var identity = fixture.App.MapGroup("/api/v1/identity");
            LoginEndpoint.Map(identity);
            LogoutEndpoint.Map(identity);
            SessionEndpoint.Map(identity);
            fixture.App.MapGet("/protected", () => "OK").RequireAuthorization();
            fixture.App.MapGet("/admin", () => "OK").RequireAuthorization("AdminOnly");
            await fixture.App.StartAsync();
            fixture.Client = new HttpClient { BaseAddress = new Uri(fixture.App.Urls.Single()), Timeout = TimeSpan.FromSeconds(15) };
            fixture.Client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            await fixture.LoginAsync();
            return fixture;
        }

        public async Task LoginAsync()
        {
            using var response = await Client.PostAsJsonAsync("/api/v1/identity/login-user", new { Email = "session@test.com", Password = "Session-Test123!" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Cookie = response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            var status = await Client.GetFromJsonAsync<SessionStatus>("/api/v1/identity/session");
            Client.DefaultRequestHeaders.Remove("X-Dima-Session");
            Client.DefaultRequestHeaders.Add("X-Dima-Session", status!.SessionId.ToString());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            foreach (var entity in builder.Model.GetEntityTypes())
                foreach (var constraint in entity.GetCheckConstraints().ToList())
                    entity.RemoveCheckConstraint(constraint.Name!);
        }
    }
    private sealed record SessionStatus(Guid SessionId, DateTime ExpiresUtc);
}
