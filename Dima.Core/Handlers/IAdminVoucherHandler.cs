using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;

namespace Dima.Core.Handlers;

public interface IAdminVoucherHandler
{
    Task<Response<Voucher?>> CreateAsync(
        CreateVoucherRequest request);

    Task<Response<Voucher?>> UpdateAsync(
        UpdateVoucherRequest request);

    Task<PagedResponse<List<Voucher>?>> GetAllForAdminAsync(
        GetAllAdminVouchersRequest request);

    Task<Response<Voucher?>> GetByIdForAdminAsync(
        GetVoucherByIdRequest request);

    Task<Response<Voucher?>> ActivateAsync(
        ActivateVoucherRequest request);

    Task<Response<Voucher?>> DeactivateAsync(
        DeactivateVoucherRequest request);
}