using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Handlers
{
    public interface IOrderHandler
    {
        Task<Response<Order?>> CancelAsync(CancelOrderRequest request);
        Task<Response<Order?>> CreateAsync(CreateOrderRequest request);
        Task<Response<Order?>> PayAsync(PayOrderRequest request);
        Task<Response<Order?>> RefundAsync(RefundOrderRequest request);
        Task<Response<List<Order>?>> GetAllAsync(GetAllOrdersRequest request);
        Task<Response<List<Order>?>> GetByNumberAsync(GetOrderByNumberRequest request);
    }
}
