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
}