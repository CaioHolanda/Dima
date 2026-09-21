using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dima.Api.Services;

public sealed class OrderExpirationWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<OrderExpirationOptions> options,
    ILogger<OrderExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na verificação de expiração em segundo plano");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds),
                    timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    public async Task SweepAsync(CancellationToken cancellationToken)
    {
        var deadline = timeProvider.GetUtcNow();
        long lastId = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<long> ids;
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                ids = await db.Orders.AsNoTracking()
                    .Where(x => x.Id > lastId && x.Status == EOrderStatus.WaitingPayment &&
                        x.ExpiresAt != null && x.ExpiresAt <= deadline)
                    .OrderBy(x => x.Id).Select(x => x.Id).Take(100)
                    .ToListAsync(cancellationToken);
            }

            if (ids.Count == 0) return;
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Cada pedido usa seu próprio contexto, inclusive após falhas.
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<OrderExpirationService>();
                var result = await service.ExpireAsync(id);
                if (!result.IsSuccess)
                    logger.LogWarning("Expiração do pedido {OrderId} bloqueada: {Reason}", id, result.Message);
            }
            // Pedidos bloqueados serão tentados novamente na próxima passagem,
            // sem impedir o processamento dos demais.
            lastId = ids[^1];
        }
    }
}
