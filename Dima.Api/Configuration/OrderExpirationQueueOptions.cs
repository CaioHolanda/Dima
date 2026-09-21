namespace Dima.Api.Configuration;

public sealed class OrderExpirationQueueOptions
{
    public const string SectionName = "OrderExpirationQueue";
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = "dima-order-expiration";
}