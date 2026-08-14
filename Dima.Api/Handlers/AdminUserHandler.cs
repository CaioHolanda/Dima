using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class AdminUserHandler(AppDbContext context)
    : IAdminUserHandler
{
    public async Task<PagedResponse<List<AdminUserListItem>?>>
        GetAllAsync(GetAllAdminUsersRequest request)
    {
        try
        {
            var now = DateTime.Now;

            var query = context.Users
                .AsNoTracking()
                .OrderBy(x => x.Email);

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(user => new AdminUserListItem
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,

                    ProductName = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt <= now)
                        .OrderByDescending(order =>
                            order.AccessEndsAt == null ||
                            order.AccessEndsAt > now)
                        .ThenByDescending(order => order.AccessStartsAt)
                        .Select(order => order.Product.Title)
                        .FirstOrDefault(),

                    AccessStartsAt = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt <= now)
                        .OrderByDescending(order =>
                            order.AccessEndsAt == null ||
                            order.AccessEndsAt > now)
                        .ThenByDescending(order => order.AccessStartsAt)
                        .Select(order => order.AccessStartsAt)
                        .FirstOrDefault(),

                    AccessEndsAt = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt <= now)
                        .OrderByDescending(order =>
                            order.AccessEndsAt == null ||
                            order.AccessEndsAt > now)
                        .ThenByDescending(order => order.AccessStartsAt)
                        .Select(order => order.AccessEndsAt)
                        .FirstOrDefault(),

                    IsPremium = context.Orders
                        .Any(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt <= now &&
                            (
                                order.AccessEndsAt == null ||
                                order.AccessEndsAt > now
                            )),

                    NextProductName = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt > now)
                        .OrderBy(order => order.AccessStartsAt)
                        .Select(order => order.Product.Title)
                        .FirstOrDefault(),

                    NextAccessStartsAt = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt > now)
                        .OrderBy(order => order.AccessStartsAt)
                        .Select(order => order.AccessStartsAt)
                        .FirstOrDefault(),

                    NextAccessEndsAt = context.Orders
                        .Where(order =>
                            order.UserId == user.Id &&
                            order.Status == EOrderStatus.Paid &&
                            order.AccessStartsAt != null &&
                            order.AccessStartsAt > now)
                        .OrderBy(order => order.AccessStartsAt)
                        .Select(order => order.AccessEndsAt)
                        .FirstOrDefault(),

                    IsActive =
                        user.LockoutEnd != DateTimeOffset.MaxValue
                })
                .ToListAsync();

            var count = await query.CountAsync();

            return new PagedResponse<List<AdminUserListItem>?>(
                users,
                count,
                request.PageNumber,
                request.PageSize);
        }
        catch
        {
            return new PagedResponse<List<AdminUserListItem>?>(
                null,
                500,
                "[E174] Não foi possível consultar os usuários");
        }
    }

    public async Task<Response<List<UserLookup>?>>
        SearchAsync(SearchUsersRequest request)
    {
        try
        {
            var searchTerm = request.SearchTerm?.Trim();

            if (string.IsNullOrWhiteSpace(searchTerm) ||
                searchTerm.Length < 2)
            {
                return new Response<List<UserLookup>?>(
                    null,
                    400,
                    "Informe pelo menos dois caracteres para pesquisar");
            }

            var limit = Math.Clamp(request.Limit, 1, 10);

            var users = await context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Email != null &&
                    x.Email.Contains(searchTerm))
                .OrderBy(x => x.Email)
                .Take(limit)
                .Select(x => new UserLookup
                {
                    Id = x.Id,
                    Email = x.Email!
                })
                .ToListAsync();

            return new Response<List<UserLookup>?>(
                users,
                200);
        }
        catch
        {
            return new Response<List<UserLookup>?>(
                null,
                500,
                "[E159] Não foi possível pesquisar os usuários");
        }
    }
}