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
    public async Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request)
    {
        try
        {
            var query =
                from order in context.Orders.AsNoTracking()
                join user in context.Users.AsNoTracking()
                    on order.UserId equals user.Id
                orderby order.CreatedAt descending
                select new AdminOrderListItem
                {
                    Id = order.Id,
                    Number = order.Number,

                    UserId = order.UserId,
                    UserEmail = user.Email ?? string.Empty,

                    ProductId = order.ProductId,
                    ProductName = order.Product.Title,

                    VoucherCode = order.Voucher != null
                        ? order.Voucher.Code
                        : null,

                    OriginalPrice = order.OriginalPrice,
                    DiscountAmount = order.DiscountAmount,
                    Total = order.Total,

                    CreatedAt = order.CreatedAt,
                    AccessStartsAt = order.AccessStartsAt,
                    AccessEndsAt = order.AccessEndsAt,

                    Status = order.Status
                };

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