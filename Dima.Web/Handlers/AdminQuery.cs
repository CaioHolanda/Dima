using Dima.Core.Requests;
namespace Dima.Web.Handlers;
public static class AdminQuery
{
    public static string Build(AdminPagedRequest request)
    {
        var query = $"?pageNumber={request.PageNumber}&pageSize={request.PageSize}&searchTerm={Uri.EscapeDataString(request.SearchTerm?.Trim() ?? string.Empty)}";
        if (request.IsActive.HasValue) query += $"&isActive={request.IsActive.Value}";
        if (request is Dima.Core.Requests.Users.GetAllAdminUsersRequest users && users.IsPremium.HasValue)
            query += $"&isPremium={users.IsPremium.Value}";
        if (request is Dima.Core.Requests.Order.GetAllAdminOrdersRequest orders && orders.Status.HasValue)
            query += $"&status={(int)orders.Status.Value}";
        return query;
    }
}
