using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IVoucherHandler
{
    Task<Response<Voucher?>> GetByCodeAsync(
        GetVoucherByCodeRequest request);
}