using Dima.Api.Data;
using Dima.Core.Common;
using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;
using Dima.Api.Services;

namespace Dima.Api.Handlers;

public class VoucherHandler(AppDbContext context, VoucherEligibilityService eligibilityService) : IVoucherHandler
{
    public async Task<Response<VoucherApplication?>> ApplyAsync(
    ApplyVoucherRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return new Response<VoucherApplication?>(
                null,
                400,
                "[E230] Informe o código do voucher");
        }

        try
        {
            var code = request.Code
                .Trim()
                .ToUpperInvariant();

            var currentUserId = await context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Email == request.UserId ||
                    x.UserName == request.UserId)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync();

            if (currentUserId is null)
            {
                return new Response<VoucherApplication?>(
                    null,
                    404,
                    "[E246] Usuário não encontrado");
            }

            var product = await context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ProductId &&
                    x.IsActive);

            if (product is null)
            {
                return new Response<VoucherApplication?>(
                    null,
                    404,
                    "[E231] Produto não encontrado");
            }

            var voucher = await context.Vouchers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Code == code);

            if (voucher is null)
            {
                return new Response<VoucherApplication?>(
                    null,
                    404,
                    "[E232] Voucher não encontrado");
            }

            var eligibility =
                await eligibilityService.EvaluateAsync(
                    voucher,
                    product,
                    currentUserId.Value,
                    DateTime.Now);

            if (!eligibility.IsEligible)
            {
                return new Response<VoucherApplication?>(
                    null,
                    400,
                    eligibility.Message);
            }
            var discountAmount =
                VoucherDiscountCalculator.Calculate(
                    product.Price,
                    voucher);

            var application = new VoucherApplication
            {
                VoucherId = voucher.Id,
                Code = voucher.Code,
                Title = voucher.Title,
                DiscountAmount = discountAmount,
                Total = product.Price - discountAmount
            };

            return new Response<VoucherApplication?>(
                application,
                200,
                "Voucher aplicado com sucesso");
        }
        catch
        {
            return new Response<VoucherApplication?>(
                null,
                500,
                "[E234] Não foi possível validar o voucher");
        }
    }
}