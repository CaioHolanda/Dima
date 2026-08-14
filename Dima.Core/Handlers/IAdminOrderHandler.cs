using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IAdminOrderHandler
{
    Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request);
}