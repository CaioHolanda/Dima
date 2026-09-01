using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IVoucherHandler
{
    Task<Response<Voucher?>> GetByCodeAsync(
        GetVoucherByCodeRequest request);
    Task<Response<VoucherApplication?>> ApplyAsync(
        ApplyVoucherRequest request);
}