using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class AdminUserHandler(AppDbContext context)
    : IAdminUserHandler
{
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