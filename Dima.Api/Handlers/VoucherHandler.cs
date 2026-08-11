using Dima.Api.Data;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers;

public class VoucherHandler(AppDbContext context) : IVoucherHandler
{
    public async Task<Response<Voucher?>> GetByCodeAsync(
        GetVoucherByCodeRequest request)
    {
        try
        {
            var code = request.Code.Trim().ToUpperInvariant();

            var voucher = await context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == code &&
                    x.IsActive);

            return voucher is null
                ? new Response<Voucher?>(
                    null,
                    404,
                    "Voucher não encontrado.")
                : new Response<Voucher?>(voucher);
        }
        catch
        {
            return new Response<Voucher?>(
                null,
                500,
                "Não foi possível recuperar o voucher.");
        }
    }
}