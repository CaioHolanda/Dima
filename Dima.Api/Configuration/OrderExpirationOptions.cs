namespace Dima.Api.Configuration;

public sealed class OrderExpirationOptions
{
    public const string SectionName = "OrderExpiration";

    public int PendingOrderLifetimeMinutes { get; set; } = 30;

    public int PaymentSessionLifetimeMinutes { get; set; } = 60;
}