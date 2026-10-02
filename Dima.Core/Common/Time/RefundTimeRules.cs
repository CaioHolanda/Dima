namespace Dima.Core.Common.Time;

public static class RefundTimeRules
{
    public static bool HasAccessStarted(DateTime accessStartsAt, DateTime utcNow) =>
        UtcInstant.Normalize(accessStartsAt) <= UtcInstant.Normalize(utcNow);

    public static bool IsWithinWindow(DateTime paidAt, DateTime utcNow) =>
        UtcInstant.Normalize(utcNow) <= UtcInstant.Normalize(paidAt).AddDays(14);
}
