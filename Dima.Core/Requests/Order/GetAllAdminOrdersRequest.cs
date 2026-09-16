namespace Dima.Core.Requests.Order;

public class GetAllAdminOrdersRequest : AdminPagedRequest
{
    public Dima.Core.Enums.EOrderStatus? Status { get; set; }
}