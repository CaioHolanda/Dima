namespace Dima.Tests;

internal sealed class FixedClock(DateTimeOffset instant, TimeZoneInfo? localZone = null) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instant.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => localZone ?? TimeZoneInfo.Utc;
}
