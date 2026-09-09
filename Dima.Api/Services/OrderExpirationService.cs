using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Services;

public sealed class OrderExpirationService(
    AppDbContext context,
    IPaymentSessionCloser sessionCloser,
    TimeProvider timeProvider,
    ILogger<OrderExpirationService> logger)
{
    public async Task<Response<bool>> ExpirePendingForUserAsync(
    string userName)
    {
        try
        {
            var userId = await context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Email == userName ||
                    x.UserName == userName)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();

            if (userId is null)
            {
                return new Response<bool>(
                    false,
                    404,
                    "[E272] Usuário não encontrado.");
            }

            var pendingOrderId = await context.Orders
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId.Value &&
                    x.Status == EOrderStatus.WaintingPayment)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();

            if (pendingOrderId is null)
            {
                return new Response<bool>(
                    false,
                    200,
                    "Não há pedido pendente para verificar.");
            }

            return await ExpireAsync(pendingOrderId.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Falha ao verificar o pedido pendente antes de uma compra");

            return new Response<bool>(
                false,
                500,
                "[E273] Não foi possível verificar o pedido pendente.");
        }
    }
    public async Task<Response<bool>> ExpireAsync(long orderId)
    {
        try
        {
            var order = await context.Orders
                .FirstOrDefaultAsync(x => x.Id == orderId);

            if (order is null)
            {
                return new Response<bool>(
                    false,
                    404,
                    "[E266] Pedido não encontrado.");
            }

            // Repetir a operação não altera a expiração já registrada.
            if (order.Status == EOrderStatus.Expired)
            {
                return new Response<bool>(
                    true,
                    200,
                    "Pedido já expirado.");
            }

            if (order.Status != EOrderStatus.WaintingPayment)
            {
                return new Response<bool>(
                    false,
                    200,
                    "O pedido não está aguardando pagamento.");
            }

            if (order.ExpiresAt is null)
            {
                return new Response<bool>(
                    false,
                    409,
                    "[E267] Pedido sem prazo de validade. " +
                    "É necessário verificar o pedido legado.");
            }

            var nowUtc = timeProvider.GetUtcNow();

            if (order.ExpiresAt.Value > nowUtc)
            {
                return new Response<bool>(
                    false,
                    200,
                    "O prazo do pedido ainda não venceu.");
            }

            if (!string.IsNullOrWhiteSpace(order.PaymentSessionId))
            {
                var closeResult = await sessionCloser.CloseAsync(
                    order.PaymentSessionId);

                if (!closeResult.IsSuccess || !closeResult.Data)
                {
                    return new Response<bool>(
                        false,
                        closeResult.IsSuccess ? 409 : closeResult.Code,
                        closeResult.Message);
                }
            }
            else if (order.PaymentSessionExpiresAt.HasValue)
            {
                // Houve preparação de uma tentativa de checkout.
                // O Stripe pode ter criado a sessão sem que seu ID
                // tenha sido salvo no banco.
                return new Response<bool>(
                    false,
                    409,
                    "[E268] Existe uma tentativa de pagamento " +
                    "cujo resultado precisa ser recuperado.");
            }

            var redemption = await context.VoucherRedemptions
                .FirstOrDefaultAsync(x => x.OrderId == order.Id);

            if (redemption?.Status ==
                EVoucherRedemptionStatus.Redeemed)
            {
                return new Response<bool>(
                    false,
                    409,
                    "[E269] O voucher deste pedido consta como " +
                    "resgatado. A situação precisa ser verificada.");
            }

            var expiredAtUtc = timeProvider.GetUtcNow();

            order.Status = EOrderStatus.Expired;
            order.ExpiredAt = expiredAtUtc;

            // Compatibilidade com os campos legados em horário local.
            order.UpdatedAt = expiredAtUtc.LocalDateTime;

            if (redemption?.Status ==
                EVoucherRedemptionStatus.Reserved)
            {
                redemption.Status =
                    EVoucherRedemptionStatus.Released;

                redemption.ReleasedAt =
                    expiredAtUtc.LocalDateTime;
            }

            // Uma única gravação: pedido e reserva são atualizados
            // na mesma transação do SaveChanges.
            await context.SaveChangesAsync();

            return new Response<bool>(
                true,
                200,
                $"Pedido {order.Number} expirado com sucesso.");
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();

            return new Response<bool>(
                false,
                409,
                "[E270] O pedido foi atualizado durante a expiração. " +
                "Sua situação precisa ser consultada novamente.");
        }
        catch (Exception ex)
        {
            context.ChangeTracker.Clear();

            logger.LogError(
                ex,
                "Falha ao expirar o pedido {OrderId}",
                orderId);

            return new Response<bool>(
                false,
                500,
                "[E271] Não foi possível concluir a expiração do pedido.");
        }
    }
}