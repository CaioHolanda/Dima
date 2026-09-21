using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IAdminOrderHandler
{
    Task<Response<bool>> CancelAsync(long id);
    Task<Response<AdminOrderDetails?>> GetByIdAsync(long id);
    Task<PagedResponse<List<AdminOrderListItem>?>>
        GetAllAsync(GetAllAdminOrdersRequest request);
}
