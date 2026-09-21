using System.Text.Json;
using Dima.Api.Services;
using Microsoft.Azure.Functions.Worker;

namespace Dima.OrderJobs;

public sealed class ExpireOrder(ScheduledOrderExpirationProcessor processor)
{
    [Function(nameof(ExpireOrder))]
    public Task Run(
        [QueueTrigger("%OrderExpirationQueueName%", Connection = "OrderExpirationStorage")] string body,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<OrderExpirationMessage>(body)
            ?? throw new InvalidOperationException("Mensagem vazia.");
        // Returning acknowledges; throwing retries and eventually preserves the message in -poison.
        return processor.ProcessAsync(message, cancellationToken);
    }
}