using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Dima.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Dima.Api.Services;

public sealed record OrderExpirationMessage(long OrderId, string OrderNumber, DateTime CreatedUtc);

public interface IOrderExpirationScheduler
{
    Task ScheduleAsync(OrderExpirationMessage message, DateTimeOffset dueAt,
        CancellationToken cancellationToken = default);
}

public sealed class QueueOrderExpirationScheduler(
    IOptions<OrderExpirationQueueOptions> options, TimeProvider clock) : IOrderExpirationScheduler
{
    public async Task ScheduleAsync(OrderExpirationMessage message, DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("OrderExpirationQueue:ConnectionString não configurada.");

        var client = new QueueClient(settings.ConnectionString, settings.QueueName,
            new QueueClientOptions
            {
                MessageEncoding = QueueMessageEncoding.Base64,
                Retry = { MaxRetries = 2, NetworkTimeout = TimeSpan.FromSeconds(10) }
            });
        // Provision the queue before activation. No create/delete permissions needed at runtime.
        var delay = GetVisibilityDelay(dueAt, clock.GetUtcNow());
        await client.SendMessageAsync(BinaryData.FromObjectAsJson(message),
            visibilityTimeout: delay, timeToLive: TimeSpan.FromSeconds(-1),
            cancellationToken: cancellationToken);
    }

    public static TimeSpan GetVisibilityDelay(DateTimeOffset dueAt, DateTimeOffset now)
        => TimeSpan.FromSeconds(Math.Clamp(Math.Ceiling((dueAt - now).TotalSeconds), 1, 604800));
}