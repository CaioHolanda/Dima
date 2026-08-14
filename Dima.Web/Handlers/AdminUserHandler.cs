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
        var response = await _client.GetAsync(
            $"v1/admin/users" +
            $"?pageSize={request.PageSize}" +
            $"&pageNumber={request.PageNumber}");

        var result = await response.Content
            .ReadFromJsonAsync<PagedResponse<List<AdminUserListItem>?>>();

        return result ??
            new PagedResponse<List<AdminUserListItem>?>(
                null,
                (int)response.StatusCode,
                "[E178] Não foi possível obter os usuários");
    }

    public async Task<Response<List<UserLookup>?>>
        SearchAsync(SearchUsersRequest request)
    {
        var searchTerm = Uri.EscapeDataString(
            request.SearchTerm?.Trim() ?? string.Empty);

        var response = await _client.GetAsync(
            $"v1/admin/users/lookup" +
            $"?searchTerm={searchTerm}" +
            $"&limit={request.Limit}");

        var result = await response.Content
            .ReadFromJsonAsync<Response<List<UserLookup>?>>();

        return result ??
            new Response<List<UserLookup>?>(
                null,
                (int)response.StatusCode,
                "[E161] Não foi possível pesquisar os usuários");
    }
    public async Task<Response<AdminUserListItem?>>
    ActivateAsync(ActivateUserRequest request)
    {
        var response = await _client.PatchAsync(
            $"v1/admin/users/{request.Id}/activate",
            null);

        var result = await response.Content
            .ReadFromJsonAsync<Response<AdminUserListItem?>>();

        return result ??
            new Response<AdminUserListItem?>(
                null,
                (int)response.StatusCode,
                "[E188] Não foi possível ativar o usuário");
    }
    public async Task<Response<AdminUserListItem?>>
    DeactivateAsync(DeactivateUserRequest request)
    {
        var response = await _client.PatchAsync(
            $"v1/admin/users/{request.Id}/deactivate",
            null);

        var result = await response.Content
            .ReadFromJsonAsync<Response<AdminUserListItem?>>();

        return result ??
            new Response<AdminUserListItem?>(
                null,
                (int)response.StatusCode,
                "[E189] Não foi possível desativar o usuário");
    }
}