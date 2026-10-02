using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Services;

// Invoked only when a durable queue message becomes visible. No SQL sweep or timer.
public sealed class ScheduledOrderExpirationProcessor(
    AppDbContext db, OrderExpirationService expiration,
    IOrderExpirationScheduler scheduler, TimeProvider clock)
{
    public async Task ProcessAsync(OrderExpirationMessage message, CancellationToken cancellationToken = default)
    {
        if (message.OrderId <= 0 || string.IsNullOrWhiteSpace(message.OrderNumber))
            throw new InvalidOperationException("Mensagem de expiração inválida.");

        var order = await db.Orders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == message.OrderId, cancellationToken);
        // A send can succeed followed by a SQL rollback, or the commit can still be in flight.
        // Retry through the queue; after the configured limit the message remains in the poison queue.
        if (order is null)
            throw new InvalidOperationException($"Pedido {message.OrderId} ausente; verificar commit ou rollback.");

        if (order.Number != message.OrderNumber || order.CreatedAt != message.CreatedUtc)
            throw new InvalidOperationException("A mensagem não corresponde à identidade do pedido.");

        if (order.Status != EOrderStatus.WaitingPayment) return;
        if (order.ExpiresAt is null)
            throw new InvalidOperationException("Pedido legado sem vencimento: revisão manual necessária.");

        if (order.ExpiresAt > clock.GetUtcNow())
        {
            // Checkout may extend the original deadline. Persist the next message before acknowledging this one.
            await scheduler.ScheduleAsync(message, order.ExpiresAt.Value, cancellationToken);
            return;
        }

        var result = await expiration.ExpireAsync(order.Id);
        if (!result.IsSuccess)
            throw new InvalidOperationException($"Expiração bloqueada para pedido {order.Id}: código {result.Code}.");

        // A concurrent checkout may have moved the deadline while the service was reading.
        db.ChangeTracker.Clear();
        var current = await db.Orders.AsNoTracking()
            .SingleAsync(x => x.Id == order.Id, cancellationToken);
        if (current.Status == EOrderStatus.WaitingPayment)
        {
            if (current.ExpiresAt > clock.GetUtcNow())
                await scheduler.ScheduleAsync(message, current.ExpiresAt.Value, cancellationToken);
            else
                throw new InvalidOperationException($"Pedido {order.Id} continua pendente; repetir com segurança.");
        }
    }
}