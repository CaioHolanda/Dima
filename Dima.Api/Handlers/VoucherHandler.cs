using Dima.Api.Data;
using Dima.Core.Common;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Vouchers;
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
                    x.Code == code &&
                    x.IsActive);

            if (voucher is null)
            {
                return new Response<VoucherApplication?>(
                    null,
                    404,
                    "[E232] Voucher não encontrado ou inativo");
            }

            if (voucher.ProductId.HasValue &&
                voucher.ProductId.Value != product.Id)
            {
                return new Response<VoucherApplication?>(
                    null,
                    400,
                    "[E233] Voucher não aplicável a este produto");
            }

            if (voucher.DiscountType ==
                    EVoucherDiscountType.FixedAmount &&
                voucher.Value > product.Price)
            {
                return new Response<VoucherApplication?>(
                    null,
                    400,
                    "[E229] O valor do voucher é superior ao valor do produto");
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