using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IAdminUserHandler
{
    Task<Response<List<UserLookup>?>>
        SearchAsync(SearchUsersRequest request);

    Task<PagedResponse<List<AdminUserListItem>?>>
        GetAllAsync(GetAllAdminUsersRequest request);

    Task<Response<AdminUserListItem?>>
        ActivateAsync(ActivateUserRequest request);

    Task<Response<AdminUserListItem?>>
        DeactivateAsync(DeactivateUserRequest request);
}