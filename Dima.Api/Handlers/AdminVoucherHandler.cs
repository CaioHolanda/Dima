using Dima.Api.Data;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class AdminVoucherHandler(AppDbContext context) : IAdminVoucherHandler
    {
        public async Task<Response<Voucher?>> ActivateAsync(
            ActivateVoucherRequest request)
        {
            try
            {
                var voucher = await context.Vouchers
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (voucher is null)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E153] Voucher não encontrado");
                }

                voucher.IsActive = true;

                await context.SaveChangesAsync();

                return new Response<Voucher?>(
                    voucher,
                    StatusCodes.Status200OK,
                    "Voucher ativado com sucesso");
            }
            catch
            {
                return new Response<Voucher?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E154] Não foi possível ativar o voucher");
            }
        }
        public async Task<Response<Voucher?>> CreateAsync(
               CreateVoucherRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E133] O código do voucher é obrigatório");
                }

                var code = request.Code
                    .Trim()
                    .ToUpperInvariant();

                var codeExists = await context.Vouchers
                    .AnyAsync(x => x.Code == code);

                if (codeExists)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status409Conflict,
                        "[E134] Já existe um voucher com este código");
                }

                if (!Enum.IsDefined(request.DiscountType))
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E139] O tipo de desconto informado é inválido");
                }

                if (request.DiscountType == EVoucherDiscountType.Percentage &&
                    request.Value > 100)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E135] O desconto percentual não pode ser maior que 100%");
                }

                if (request.StartsAt.HasValue &&
                    request.EndsAt.HasValue &&
                    request.EndsAt.Value <= request.StartsAt.Value)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E136] A data final deve ser posterior à data inicial");
                }

                if (request.ProductId.HasValue)
                {
                    var productExists = await context.Products
                        .AnyAsync(x => x.Id == request.ProductId.Value);

                    if (!productExists)
                    {
                        return new Response<Voucher?>(
                            null,
                            StatusCodes.Status400BadRequest,
                            "[E137] O produto informado não foi encontrado");
                    }
                }

                var voucher = new Voucher
                {
                    Code = code,
                    Title = request.Title,
                    Description = request.Description,
                    DiscountType = request.DiscountType,
                    Value = request.Value,
                    StartsAt = request.StartsAt,
                    EndsAt = request.EndsAt,
                    MaxTotalUses = request.MaxTotalUses,
                    MaxUsesPerUser = request.MaxUsesPerUser,
                    AssignedUserId = request.AssignedUserId,
                    ProductId = request.ProductId,
                    IsActive = request.IsActive
                };

                await context.Vouchers.AddAsync(voucher);
                await context.SaveChangesAsync();

                return new Response<Voucher?>(
                    voucher,
                    StatusCodes.Status201Created,
                    "Voucher criado com sucesso");
            }
            catch
            {
                return new Response<Voucher?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E138] Não foi possível criar o voucher");
            }
        }

        public async Task<Response<Voucher?>> DeactivateAsync(
            DeactivateVoucherRequest request)
        {
            try
            {
                var voucher = await context.Vouchers
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (voucher is null)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E151] Voucher não encontrado");
                }

                voucher.IsActive = false;

                await context.SaveChangesAsync();

                return new Response<Voucher?>(
                    voucher,
                    StatusCodes.Status200OK,
                    "Voucher desativado com sucesso");
            }
            catch
            {
                return new Response<Voucher?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E152] Não foi possível desativar o voucher");
            }
        }

        public async Task<PagedResponse<List<Voucher>?>> GetAllForAdminAsync(
            GetAllAdminVouchersRequest request)
        {
            try
            {
                var query = context.Vouchers
                    .AsNoTracking()
                    .OrderByDescending(x => x.IsActive)
                    .ThenBy(x => x.Code);

                var vouchers = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                var count = await query.CountAsync();

                return new PagedResponse<List<Voucher>?>(
                    vouchers,
                    count,
                    request.PageNumber,
                    request.PageSize);
            }
            catch
            {
                return new PagedResponse<List<Voucher>?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E140] Não foi possível consultar os vouchers");
            }
        }

        public async Task<Response<Voucher?>> GetByIdForAdminAsync(
            GetVoucherByIdRequest request)
        {
            try
            {
                var voucher = await context.Vouchers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                return voucher is null
                    ? new Response<Voucher?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E141] Voucher não encontrado")
                    : new Response<Voucher?>(voucher);
            }
            catch
            {
                return new Response<Voucher?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E142] Não foi possível consultar o voucher");
            }
        }

        public async Task<Response<Voucher?>> UpdateAsync(
            UpdateVoucherRequest request)
        {
            try
            {
                var voucher = await context.Vouchers
                    .FirstOrDefaultAsync(x => x.Id == request.Id);

                if (voucher is null)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status404NotFound,
                        "[E143] Voucher não encontrado");
                }

                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E144] O código do voucher é obrigatório");
                }

                var code = request.Code
                    .Trim()
                    .ToUpperInvariant();

                var codeExists = await context.Vouchers
                    .AnyAsync(x =>
                        x.Code == code &&
                        x.Id != request.Id);

                if (codeExists)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status409Conflict,
                        "[E145] Já existe outro voucher com este código");
                }

                if (!Enum.IsDefined(request.DiscountType))
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E146] O tipo de desconto informado é inválido");
                }

                if (request.DiscountType == EVoucherDiscountType.Percentage &&
                    request.Value > 100)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E147] O desconto percentual não pode ser maior que 100%");
                }

                if (request.StartsAt.HasValue &&
                    request.EndsAt.HasValue &&
                    request.EndsAt.Value <= request.StartsAt.Value)
                {
                    return new Response<Voucher?>(
                        null,
                        StatusCodes.Status400BadRequest,
                        "[E148] A data final deve ser posterior à data inicial");
                }

                if (request.ProductId.HasValue)
                {
                    var productExists = await context.Products
                        .AnyAsync(x => x.Id == request.ProductId.Value);

                    if (!productExists)
                    {
                        return new Response<Voucher?>(
                            null,
                            StatusCodes.Status400BadRequest,
                            "[E149] O produto informado não foi encontrado");
                    }
                }

                voucher.Code = code;
                voucher.Title = request.Title;
                voucher.Description = request.Description;
                voucher.DiscountType = request.DiscountType;
                voucher.Value = request.Value;
                voucher.StartsAt = request.StartsAt;
                voucher.EndsAt = request.EndsAt;
                voucher.MaxTotalUses = request.MaxTotalUses;
                voucher.MaxUsesPerUser = request.MaxUsesPerUser;
                voucher.AssignedUserId = request.AssignedUserId;
                voucher.ProductId = request.ProductId;
                voucher.IsActive = request.IsActive;

                await context.SaveChangesAsync();

                return new Response<Voucher?>(
                    voucher,
                    StatusCodes.Status200OK,
                    "Voucher atualizado com sucesso");
            }
            catch
            {
                return new Response<Voucher?>(
                    null,
                    StatusCodes.Status500InternalServerError,
                    "[E150] Não foi possível atualizar o voucher");
            }
        }
    }
}
