namespace Dima.Core.Common.Time;

/// <summary>Instants use UTC; civil dates must not pass through this helper.</summary>
public static class UtcInstant
{
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Local => value.ToUniversalTime(),
        // SQL DATETIME2 loses Kind. Its instant columns contain UTC after DT-18.
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value
    };

    public static string FormatLocal(DateTime? value, string format,
        TimeZoneInfo? timeZone = null) => value.HasValue
        ? TimeZoneInfo.ConvertTimeFromUtc(Normalize(value.Value), timeZone ?? TimeZoneInfo.Local).ToString(format)
        : "-";
}
