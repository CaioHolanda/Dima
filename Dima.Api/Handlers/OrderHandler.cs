using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;
using System.Collections.Immutable;

namespace Dima.Api.Handlers
{
    public class OrderHandler(
        AppDbContext context,
        IPaymentHandler paymentHandler) : IOrderHandler,IOrderPaymentConfirmationHandler
    {
        public async Task<Response<Order?>> CancelAsync(CancelOrderRequest request)
        {
            Order? order;
            // Primeira analise: Pedido pode ser cancelado?
            var userId = await GetUserIdAsync(request.UserId);

            if (userId is null)
                return new Response<Order?>(
                    null,
                    404,
                    "[E166] Usuario nao encontrado");
            try
            {

                order = await context
                    .Orders
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x =>
                            x.Id == request.Id &&
                            x.UserId == userId.Value);
                if (order is null)
                    return new Response<Order?>(null, 404, "[E035] Pedido nao encontrado");
            }
            catch 
            {
                return new Response<Order?>(null, 404, "[E036] Falha ao obter o pedido");
            }
            switch(order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "[E037] Pedido ja cancelado");
                case EOrderStatus.WaintingPayment:
                    break;
                case EOrderStatus.Paid:
                    return new Response<Order?>(order, 400, "[E038] Pedido ja pago nao pode ser cancelado");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "[E039] Pedido ja reembolsado nao pode ser cancelado");
                default:
                    return new Response<Order?>(order, 400, "[E040] Pedido nao pode ser cancelado");
            }
            order.Status = EOrderStatus.Canceled;
            order.UpdatedAt = DateTime.Now;

            // Segunda analise: Podendo ser cancelado atualiza o banco
            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch 
            {
                return new Response<Order?>(order, 500, "[E041] Nao foi possivel cancelar seu pedido");
            }
            return new Response<Order?>(order, 200, $"Pedido {order.Number} cancelado com sucesso");
        }

        public async Task<Response<Order?>> ConfirmPaymentAsync(
            string orderNumber,
            string externalReference,
            long amountReceived,
            string currency,
            string paymentUserId)
        {
            Order? order;

            try
            {
                order = await context.Orders
                    .Include(x => x.Product)
                    .FirstOrDefaultAsync(x => x.Number == orderNumber);

                if (order is null)
                {
                    return new Response<Order?>(
                        null,
                        404,
                        "[E201] Pedido nao encontrado");
                }
            }
            catch
            {
                return new Response<Order?>(
                    null,
                    500,
                    "[E202] Falha ao buscar pedido");
            }

            if (string.IsNullOrWhiteSpace(externalReference))
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E205] Referencia externa do pagamento nao informada");
            }
            if (!long.TryParse(paymentUserId, out var stripeUserId))
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E208] Identificacao do usuario no pagamento invalida");
            }

            if (stripeUserId != order.UserId)
            {
                return new Response<Order?>(
                    order,
                    409,
                    "[E209] Pagamento nao pertence ao usuario do pedido");
            }
            var expectedAmount = (long)Math.Round(
                                    order.Total * 100,
                                    0);

            if (amountReceived != expectedAmount)
            {
                return new Response<Order?>(
                    order,
                    409,
                    "[E210] Valor recebido nao corresponde ao valor do pedido");
            }
            if (!string.Equals(
                    currency,
                    "brl",
                    StringComparison.OrdinalIgnoreCase))
            {
                return new Response<Order?>(
                    order,
                    409,
                    "[E211] Moeda do pagamento nao corresponde a moeda do pedido");
            }
            if (order.Status == EOrderStatus.Paid)
            {
                if (order.ExternalReference == externalReference)
                {
                    return new Response<Order?>(
                        order,
                        200,
                        $"Pedido {order.Number} ja confirmado anteriormente");
                }

                return new Response<Order?>(
                    order,
                    409,
                    "[E206] Pedido ja pago com outra referencia externa");
            }

            if (order.Status != EOrderStatus.WaintingPayment)
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E203] Pedido nao esta aguardando pagamento");
            }


            var now = DateTime.Now;

            order.Status = EOrderStatus.Paid;
            order.ExternalReference = externalReference;
            order.PaidAt = now;
            order.UpdatedAt = now;

            var currentAccessEndsAt = await context.Orders
                .AsNoTracking()
                .Where(x =>
                    x.UserId == order.UserId &&
                    x.Status == EOrderStatus.Paid &&
                    x.AccessEndsAt != null &&
                    x.AccessEndsAt > now)
                .MaxAsync(x => (DateTime?)x.AccessEndsAt);

            var accessStartsAt = currentAccessEndsAt ?? now;

            order.AccessStartsAt = accessStartsAt;

            order.AccessEndsAt =
                order.Product.AccessDurationMonths.HasValue
                    ? accessStartsAt.AddMonths(
                        order.Product.AccessDurationMonths.Value)
                    : null;

            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch
            {
                return new Response<Order?>(
                    order,
                    500,
                    "[E204] Falha ao confirmar pagamento");
            }

            return new Response<Order?>(
                order,
                200,
                $"Pedido {order.Number} pago com sucesso");
        }

        public async Task<Response<Order?>> ConfirmRefundAsync(
            string paymentIntentId,
            string refundId,
            string refundStatus,
            string? failureReason)
        {
            Order? order;

            try
            {
                order = await context
                    .Orders
                    .FirstOrDefaultAsync(x =>
                        x.ExternalReference == paymentIntentId);
            }
            catch
            {
                return new Response<Order?>(
                    null,
                    500,
                    "[E221] Falha ao buscar pedido para confirmacao do reembolso");
            }

            if (order is null)
            {
                return new Response<Order?>(
                    null,
                    404,
                    "[E222] Pedido associado ao pagamento nao encontrado");
            }

            if (string.IsNullOrWhiteSpace(order.RefundReference))
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E223] Referencia de reembolso nao encontrada no pedido");
            }

            if (!string.Equals(
                    order.RefundReference,
                    refundId,
                    StringComparison.Ordinal))
            {
                return new Response<Order?>(
                    order,
                    409,
                    "[E224] Referencia de reembolso nao corresponde ao pedido");
            }

            var now = DateTime.Now;

            switch (refundStatus)
            {
                case "succeeded":
                    order.Status = EOrderStatus.Refunded;
                    order.RefundedAt ??= now;
                    order.RefundFailureReason = null;
                    break;

                case "pending":
                case "requires_action":
                    order.Status = EOrderStatus.RefundPending;
                    break;

                case "failed":
                case "canceled":
                    order.Status = EOrderStatus.Paid;
                    order.RefundFailureReason =
                        string.IsNullOrWhiteSpace(failureReason)
                            ? refundStatus
                            : failureReason;
                    break;

                default:
                    return new Response<Order?>(
                        order,
                        400,
                        $"[E225] Status de reembolso desconhecido: {refundStatus}");
            }

            order.UpdatedAt = now;

            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch
            {
                return new Response<Order?>(
                    order,
                    500,
                    "[E226] Falha ao atualizar estado do reembolso");
            }

            return new Response<Order?>(
                order,
                200,
                $"Reembolso do pedido {order.Number} atualizado para {refundStatus}");
        }

        public async Task<Response<Order?>> CreateAsync(CreateOrderRequest request)
        {
            var userId = await GetUserIdAsync(request.UserId);
            if (userId is null)
                return new Response<Order?>(
                    null,
                    404,
                    "[E167] Usuario nao encontrado");

            var now = DateTime.Now;

            // Já existe um pedido aguardando pagamento?
            var hasPendingOrder = await context.Orders
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId.Value &&
                    x.Status == EOrderStatus.WaintingPayment);

            if (hasPendingOrder)
            {
                return new Response<Order?>(
                    null,
                    400,
                    "[E175] Já existe um pedido aguardando pagamento");
            }

            // Já existe um plano futuro pago/agendado?
            var hasScheduledPlan = await context.Orders
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId.Value &&
                    (x.Status == EOrderStatus.Paid ||
                     x.Status == EOrderStatus.RefundPending) &&
                    x.AccessStartsAt != null &&
                    x.AccessStartsAt > now);

            if (hasScheduledPlan)
            {
                return new Response<Order?>(
                    null,
                    400,
                    "[E176] Já existe um próximo plano agendado");
            }

            // Existe acesso vitalício já adquirido?
            var hasLifetimeAccess = await context.Orders
                .AsNoTracking()
                .AnyAsync(x =>
                    x.UserId == userId.Value &&
                    (x.Status == EOrderStatus.Paid ||
                     x.Status == EOrderStatus.RefundPending) &&
                    x.AccessStartsAt != null &&
                    x.AccessEndsAt == null);

            if (hasLifetimeAccess)
            {
                return new Response<Order?>(
                    null,
                    400,
                    "[E177] O usuário já possui acesso vitalício");
            }           
            
            // Produto existe?
            Product? product;
            try
            {
                product = await context
                    .Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == request.ProductId &&
                        x.IsActive == true);
                if (product is null)
                    return new Response<Order?>(null, 404, "[E041] Produto nao encontrado");
                context.Attach(product);
            }
            catch
            {
                return new Response<Order?>(null, 500, "[E042] Nao foi possivel buscar produto");
            }

            // Ha Voucher?
            Voucher? voucher=null;
            try
            {
                if (request.VoucherId is not null)
                {
                    var voucherId = request.VoucherId.Value;

                    // 1. Existe algum voucher com este ID?
                    voucher = await context.Vouchers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == voucherId);

                    if (voucher is null)
                        return new Response<Order?>(null, 400, $"[E043] Voucher {voucherId} nao encontrado");

                    // 2. O voucher está ativo?
                    if (!voucher.IsActive)
                        return new Response<Order?>(null, 400, $"[E043] Voucher {voucherId} existe, mas esta inativo");

                    // 3. Agora sim, atualiza
                    voucher.IsActive = false;
                    context.Vouchers.Update(voucher);
                }
            }
            catch 
            {
                return new Response<Order?>(null, 500, "[E045] Falha ao obter o Voucher informado");
            }

            // Se existe produto e ha ou nao voucher cria-se o pedido
            var originalPrice = product.Price;
            var discountAmount = 0m;
            var total = originalPrice;

            var order = new Order
            {
                UserId = userId.Value,

                Product = product,
                ProductId = request.ProductId,

                Voucher = voucher,
                VoucherId = request.VoucherId,

                OriginalPrice = originalPrice,
                DiscountAmount = discountAmount,
                Total = total
            };
            try
            {
                await context.Orders.AddAsync(order);
                await context.SaveChangesAsync();
            }
            catch 
            {
                return new Response<Order?>(null, 500, "[E046] Nao foi possivel realizar seu pedido");
            }

            return new Response<Order?>(order, 201, $"Pedido {order.Number} cadastrado com sucesso");
        }

        public async Task<PagedResponse<List<Order>?>> GetAllAsync(GetAllOrdersRequest request)
        {
            var userId = await GetUserIdAsync(request.UserId);

            if (userId is null)
                return new PagedResponse<List<Order>?>(
                    null,
                    404,
                    "[E169] Usuario nao encontrado");
            try
            {
                var query = context
                    .Orders
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .Where(x => x.UserId == userId.Value)
                    .OrderByDescending(x => x.CreatedAt);
                var orders = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();
                var count = await query.CountAsync();
                return new PagedResponse<List<Order>?>(
                    orders,
                    count,
                    request.PageNumber,
                    request.PageSize);
            }
            catch 
            {
                return new PagedResponse<List<Order>?>(null, 500, "[E062] Nao foi possivel listar os pedidos");
            }

        }

        public async Task<Response<Order?>> GetByNumberAsync(GetOrderByNumberRequest request)
        {
            var userId = await GetUserIdAsync(request.UserId);

            if (userId is null)
                return new Response<Order?>(
                    null,
                    404,
                    "[E168] Usuario nao encontrado");
            try
            {
                var order = await context
                    .Orders
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x => x.Number == request.Number &&
                                              x.UserId==userId.Value);
                return order is null
                    ? new Response<Order?>(null, 404, "[E063] Pedido nao encontrado")
                    : new Response<Order?>(order);

            }
            catch 
            {

                return new Response<Order?> (null, 500, "[E061] Nao foi possivel consultar pedido");
            }
        }

        public async Task<Response<Order?>> RefundAsync(RefundOrderRequest request)
        {
            var userId = await GetUserIdAsync(request.UserId);

            if (userId is null)
                return new Response<Order?>(
                    null,
                    404,
                    "[E171] Usuario nao encontrado");
            Order? order;
            try
            {
                order = await context
                    .Orders
                    .Include(x => x.Product)
                    .Include(x => x.Voucher)
                    .FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId.Value);
                if (order is null)
                    return new Response<Order?>(null, 404, "[E060] Pedido nao encontrado");

            }
            catch
            {

                return new Response<Order?>(null, 500, "[E054] Falha ao buscar pedido");
            }
            switch (order.Status)
            {
                case EOrderStatus.Canceled:
                    return new Response<Order?>(order, 400, "[E055] Pedido cancelado, reembolso nao possivel");
                case EOrderStatus.Refunded:
                    return new Response<Order?>(order, 400, "[E056] Pedido reembolsado, reembolso nao possivel");
                case EOrderStatus.Paid:
                    break;
                case EOrderStatus.WaintingPayment:
                    return new Response<Order?>(order, 400, "[E057] Pedido ainda nao foi pago, reembolso nao possivel");
                default:
                    return new Response<Order?>(order, 400, "[E058] Falha ao processar pagamento");
            }

            if (order.AccessStartsAt is null)
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E220] Data de inicio do acesso nao encontrada");
            }

            var now = DateTime.Now;

            var accessHasStarted =
                order.AccessStartsAt.HasValue &&
                order.AccessStartsAt.Value <= now;

            if (accessHasStarted)
            {
                if (order.PaidAt is null)
                {
                    return new Response<Order?>(
                        order,
                        400,
                        "[E213] Data de confirmacao do pagamento nao encontrada");
                }

                var refundDeadline =
                    order.PaidAt.Value.AddDays(14);

                if (now > refundDeadline)
                {
                    return new Response<Order?>(
                        order,
                        400,
                        "[E214] Prazo de 14 dias para reembolso expirado");
                }
            }

            if (string.IsNullOrWhiteSpace(order.ExternalReference))
            {
                return new Response<Order?>(
                    order,
                    400,
                    "[E219] Referencia externa do pagamento nao encontrada");
            }

            if (request.RefundReason is null)
                return new Response<Order?>(
                    null,
                    400,
                    "[E226] Motivo do reembolso não informado");

            if (request.RefundReason == ERefundReason.Other &&
                string.IsNullOrWhiteSpace(request.RefundReasonDetails))
            {
                return new Response<Order?>(
                    null,
                    400,
                    "[E227] Informe o motivo do reembolso");
            }
            order.RefundReason = request.RefundReason;
            order.RefundReasonDetails =
                string.IsNullOrWhiteSpace(request.RefundReasonDetails)
                    ? null
                    : request.RefundReasonDetails.Trim();

            var refundResult = await paymentHandler.RefundAsync(
                order.ExternalReference,
                $"refund-order-{order.Id}");

            if (!refundResult.IsSuccess)
            {
                return new Response<Order?>(
                    order,
                    refundResult.Code,
                    refundResult.Message);
            }


            order.RefundReference = refundResult.Data;
            order.RefundFailureReason = null;
            order.Status = EOrderStatus.RefundPending;
            order.UpdatedAt = DateTime.Now;

            // Persistencia em banco
            try
            {
                context.Orders.Update(order);
                await context.SaveChangesAsync();
            }
            catch
            {
                return new Response<Order?>(order, 500, "[E059] Falha ao processar reembolso");
            }
            return new Response<Order?>(order, 200, $"Reembolso do pedido {order.Number} solicitado com sucesso");
        }

        private async Task<long?> GetUserIdAsync(string userIdentifier)
        {
            return await context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Email == userIdentifier ||
                    x.UserName == userIdentifier)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();
        }
    }
}
