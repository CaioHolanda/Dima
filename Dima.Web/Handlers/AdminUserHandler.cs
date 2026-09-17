using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers;

public class AdminUserHandler(
    IHttpClientFactory httpClientFactory)
    : IAdminUserHandler
{
    private readonly HttpClient _client =
        httpClientFactory.CreateClient(
            Configuration.HttpClientName);

    public async Task<PagedResponse<List<AdminUserListItem>?>>
    GetAllAsync(GetAllAdminUsersRequest request)
    {
        using var response = await _client.GetAsync(
            $"v1/admin/users" +
            AdminQuery.Build(request));

        return await HttpResponseReader.ReadPagedAsync<List<AdminUserListItem>?>(response, "[E178] Não foi possível obter os usuários");
    }

    public async Task<Response<List<UserLookup>?>>
        SearchAsync(SearchUsersRequest request)
    {
        var searchTerm = Uri.EscapeDataString(
            request.SearchTerm?.Trim() ?? string.Empty);

        using var response = await _client.GetAsync(
            $"v1/admin/users/lookup" +
            $"?searchTerm={searchTerm}" +
            $"&limit={request.Limit}");

        return await HttpResponseReader.ReadAsync<List<UserLookup>?>(response, "[E161] Não foi possível pesquisar os usuários");
    }
    public async Task<Response<AdminUserListItem?>>
    ActivateAsync(ActivateUserRequest request)
    {
        using var response = await _client.PatchAsync(
            $"v1/admin/users/{request.Id}/activate",
            null);

        return await HttpResponseReader.ReadAsync<AdminUserListItem?>(response, "[E188] Não foi possível ativar o usuário");
    }
    public async Task<Response<AdminUserListItem?>>
    DeactivateAsync(DeactivateUserRequest request)
    {
        using var response = await _client.PatchAsync(
            $"v1/admin/users/{request.Id}/deactivate",
            null);

        return await HttpResponseReader.ReadAsync<AdminUserListItem?>(response, "[E189] Não foi possível desativar o usuário");
    }
}