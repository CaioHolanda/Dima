using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dima.Core.Security;
using System.Data;
using IdentityUser = Dima.Api.Models.User;

namespace Dima.Api.Handlers;

public class AdminUserHandler(AppDbContext context,
                              UserManager<IdentityUser> userManager)
            : IAdminUserHandler
{
    public async Task<Response<AdminUserListItem?>>
        ActivateAsync(ActivateUserRequest request)
    {
        try
        {
            var user = await userManager.FindByIdAsync(
                request.Id.ToString());

            if (user is null)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    404,
                    "[E183] Usuário não encontrado");
            }

            if (user.LockoutEnd != DateTimeOffset.MaxValue)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    400,
                    "[E184] O usuário já está ativo");
            }

            var result = await userManager.SetLockoutEndDateAsync(
                user,
                null);

            if (!result.Succeeded)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    400,
                    "[E185] Não foi possível ativar o usuário");
            }

            return new Response<AdminUserListItem?>(
                new AdminUserListItem
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    IsActive = true
                },
                200,
                "Usuário ativado com sucesso");
        }
        catch
        {
            return new Response<AdminUserListItem?>(
                null,
                500,
                "[E186] Não foi possível ativar o usuário");
        }
    }

    public async Task<Response<AdminUserListItem?>>
        DeactivateAsync(DeactivateUserRequest request)
    {
        try
        {
            // Serialize the administrator count and update across API instances.
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

            var user = await userManager.FindByIdAsync(
                request.Id.ToString());

            if (user is null)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    404,
                    "[E179] Usuário não encontrado");
            }

            if (request.ActorId <= 0 || request.ActorId == user.Id)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    403,
                    "[E180] Não é permitido desativar a própria conta");
            }

            if (user.LockoutEnd == DateTimeOffset.MaxValue)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    400,
                    "[E181] O usuário já está desativado");
            }

            if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
            {
                var now = DateTimeOffset.UtcNow;
                var hasOtherActiveAdmin = await (
                    from candidate in context.Users
                    join membership in context.UserRoles on candidate.Id equals membership.UserId
                    join role in context.Roles on membership.RoleId equals role.Id
                    where role.NormalizedName == AppRoles.Admin.ToUpperInvariant()
                        && candidate.Id != user.Id
                        && candidate.EmailConfirmed
                        && (!candidate.LockoutEnabled || candidate.LockoutEnd == null
                            || candidate.LockoutEnd <= now)
                    select candidate.Id).AnyAsync();

                if (!hasOtherActiveAdmin)
                    return new Response<AdminUserListItem?>(null, 403,
                        "[E188] O último administrador ativo não pode ser desativado");
            }

            // Persist lockout and stamp together; failure must not partially deactivate.
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            var result = await userManager.UpdateSecurityStampAsync(user);

            if (!result.Succeeded)
            {
                return new Response<AdminUserListItem?>(
                    null,
                    400,
                    "[E182] Não foi possível desativar o usuário");
            }

            await transaction.CommitAsync();

            return new Response<AdminUserListItem?>(
                new AdminUserListItem
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    IsActive = false
                },
                200,
                "Usuário desativado com sucesso");
        }
        catch
        {
            return new Response<AdminUserListItem?>(
                null,
                500,
                "[E183] Não foi possível desativar o usuário");
        }
    }
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
                            order.AccessEndsAt != null &&
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
                            order.AccessEndsAt != null &&
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
                            order.AccessEndsAt != null &&
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
                            order.AccessEndsAt != null &&
                            order.AccessEndsAt > now),

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