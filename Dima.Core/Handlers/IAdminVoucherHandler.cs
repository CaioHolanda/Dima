using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using Dima.Core.Models.Vouchers;

namespace Dima.Core.Handlers;

public interface IAdminVoucherHandler
{
    Task<Response<Voucher?>> CreateAsync(CreateVoucherRequest request);

    Task<Response<Voucher?>> UpdateAsync(UpdateVoucherRequest request);

    Task<PagedResponse<List<AdminVoucherListItem>?>> GetAllForAdminAsync(GetAllAdminVouchersRequest request);

    Task<Response<AdminVoucherDetails?>> GetByIdForAdminAsync(GetVoucherByIdRequest request);

    Task<Response<Voucher?>> ActivateAsync(ActivateVoucherRequest request);

    Task<Response<Voucher?>> DeactivateAsync(DeactivateVoucherRequest request);
}