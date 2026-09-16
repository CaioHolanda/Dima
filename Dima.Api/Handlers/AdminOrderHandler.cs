using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class AdminOrderHandler(AppDbContext context)
    : IAdminOrderHandler
{
    public async Task<Response<AdminOrderDetails?>> GetByIdAsync(long id)
    {
        try
        {
            var order = await (
                from item in context.Orders.AsNoTracking()
                join user in context.Users.AsNoTracking() on item.UserId equals user.Id
                where item.Id == id
                select new AdminOrderDetails
                {
                    Id = item.Id, Number = item.Number,
                    UserId = item.UserId, UserEmail = user.Email ?? string.Empty,
                    ProductId = item.ProductId, ProductName = item.Product.Title,
                    VoucherCode = item.VoucherCodeSnapshot,
                    VoucherDiscountTypeSnapshot = item.VoucherDiscountTypeSnapshot,
                    VoucherValueSnapshot = item.VoucherValueSnapshot,
                    OriginalPrice = item.OriginalPrice, DiscountAmount = item.DiscountAmount, Total = item.Total,
                    CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt, Status = item.Status,
                    PaidAt = item.PaidAt, Gateway = item.Gateway, ExternalReference = item.ExternalReference,
                    PaymentSessionId = item.PaymentSessionId, PaymentSessionExpiresAt = item.PaymentSessionExpiresAt,
                    ExpiresAt = item.ExpiresAt, ExpiredAt = item.ExpiredAt,
                    AccessStartsAt = item.AccessStartsAt, AccessEndsAt = item.AccessEndsAt,
                    AccessDurationMonths = item.AccessDurationMonths,
                    RefundReference = item.RefundReference, RefundFailureReason = item.RefundFailureReason,
                    RefundedAt = item.RefundedAt, RefundReason = item.RefundReason,
                    RefundReasonDetails = item.RefundReasonDetails
                }).SingleOrDefaultAsync();
            return order is null
                ? new Response<AdminOrderDetails?>(null, 404, "Pedido não encontrado.")
                : new Response<AdminOrderDetails?>(order);
        }
        catch
        {
            return new Response<AdminOrderDetails?>(null, 500, "Não foi possível consultar o pedido.");
        }
    }

    public async Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request)
    {
        try
        {
            var query =
                from order in context.Orders.AsNoTracking()
                join user in context.Users.AsNoTracking()
                    on order.UserId equals user.Id
                orderby order.CreatedAt descending, order.Id descending
                select new AdminOrderListItem
                {
                    Id = order.Id,
                    Number = order.Number,

                    UserId = order.UserId,
                    UserEmail = user.Email ?? string.Empty,

                    ProductId = order.ProductId,
                    ProductName = order.Product.Title,

                    VoucherCode = order.VoucherCodeSnapshot,

                    OriginalPrice = order.OriginalPrice,
                    DiscountAmount = order.DiscountAmount,
                    Total = order.Total,

                    CreatedAt = order.CreatedAt,
                    AccessStartsAt = order.AccessStartsAt,
                    AccessEndsAt = order.AccessEndsAt,

                    Status = order.Status,

                    PaidAt = order.PaidAt,

                    RefundReference = order.RefundReference,
                    RefundFailureReason = order.RefundFailureReason,
                    RefundedAt = order.RefundedAt,
                    RefundReason = order.RefundReason,
                    RefundReasonDetails = order.RefundReasonDetails,
                };

            if (request.Status.HasValue)
                query = query.Where(x => x.Status == request.Status.Value);
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(x => x.Number.ToLower().Contains(term)
                    || x.UserEmail.ToLower().Contains(term) || x.ProductName.ToLower().Contains(term));
            }
            var count = await query.CountAsync();

            var orders = await query
                .Skip(
                    (request.PageNumber - 1) *
                    request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResponse<List<AdminOrderListItem>?>(
                orders,
                count,
                request.PageNumber,
                request.PageSize);
        }
        catch
        {
            return new PagedResponse<List<AdminOrderListItem>?>(
                null,
                500,
                "[E190] Não foi possível listar os pedidos");
        }
    }
}