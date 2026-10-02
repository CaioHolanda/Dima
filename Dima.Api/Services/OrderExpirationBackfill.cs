using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Services;

// Explicit, one-off migration/recovery command. Never registered as a hosted service.
public sealed class OrderExpirationBackfill(AppDbContext db, IOrderExpirationScheduler scheduler)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        long lastId = 0;
        var scheduled = 0;
        while (true)
        {
            var batch = await db.Orders.AsNoTracking()
                .Where(x => x.Id > lastId && x.Status == EOrderStatus.WaitingPayment && x.ExpiresAt != null)
                .OrderBy(x => x.Id).Take(100).ToListAsync(cancellationToken);
            if (batch.Count == 0) return scheduled;
            foreach (var order in batch)
            {
                await scheduler.ScheduleAsync(new(order.Id, order.Number, order.CreatedAt),
                    order.ExpiresAt!.Value, cancellationToken);
                scheduled++;
            }
            lastId = batch[^1].Id;
        }
    }
}