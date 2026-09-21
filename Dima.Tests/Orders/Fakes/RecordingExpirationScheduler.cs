using Dima.Api.Services;

namespace Dima.Tests.Orders.Fakes;

public sealed class RecordingExpirationScheduler : IOrderExpirationScheduler
{
    public List<(OrderExpirationMessage Message, DateTimeOffset DueAt)> Messages { get; } = [];
    public bool Fail { get; set; }
    public Task ScheduleAsync(OrderExpirationMessage message, DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Fail) throw new IOException("Queue unavailable");
        Messages.Add((message, dueAt));
        return Task.CompletedTask;
    }
}