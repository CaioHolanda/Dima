using Dima.Api.Data;
using Dima.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Services;

public sealed class UserSessionService(AppDbContext db, TimeProvider clock)
{
    public const string CookieKey = "dima.session";
    public static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan AbsoluteTimeout = TimeSpan.FromHours(8);

    public async Task<Guid> CreateAsync()
    {
        var now = clock.GetUtcNow().UtcDateTime;
        // Expired records are unusable; prune them on subsequent logins.
        if (db.Database.IsRelational())
            await db.UserSessions.Where(x => x.CreatedUtc <= now - AbsoluteTimeout).ExecuteDeleteAsync();
        var session = new UserSession { Id = Guid.NewGuid(), CreatedUtc = now, LastActivityUtc = now };
        db.UserSessions.Add(session);
        await db.SaveChangesAsync();
        return session.Id;
    }

    public async Task<DateTime?> GetExpiryAsync(Guid id)
    {
        var session = await db.UserSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (session is null) return null;
        var expiry = new[] { session.CreatedUtc + AbsoluteTimeout, session.LastActivityUtc + IdleTimeout }.Min();
        return clock.GetUtcNow().UtcDateTime < expiry ? DateTime.SpecifyKind(expiry, DateTimeKind.Utc) : null;
    }

    public async Task<bool> TouchAsync(Guid id)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        // A conditional update prevents concurrent requests from reviving an expired session.
        return await db.UserSessions
            .Where(x => x.Id == id && x.CreatedUtc > now - AbsoluteTimeout && x.LastActivityUtc > now - IdleTimeout)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.LastActivityUtc,
                x => x.LastActivityUtc > now ? x.LastActivityUtc : now)) == 1;
    }

    public Task<int> RevokeAsync(Guid id) => db.UserSessions.Where(x => x.Id == id).ExecuteDeleteAsync();
}
