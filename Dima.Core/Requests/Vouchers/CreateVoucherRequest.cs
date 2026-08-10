using Dima.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dima.Core.Requests.Vouchers;

public class CreateVoucherRequest
{
    [Required(ErrorMessage = "O código  do voucher é obrigatório")]
    [StringLength(
        20,
        MinimumLength = 4,
        ErrorMessage = "O código do voucher deve possuir entre 4 e 20 caracteres")]
    [RegularExpression(
    @"^[A-Za-z0-9]+$",
        ErrorMessage = "O código do voucher deve conter apenas letras e números")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "O título é obrigatório")]
    [MaxLength(
        80,
        ErrorMessage = "O título deve ter no máximo 80 caracteres")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória")]
    [MaxLength(
        255,
        ErrorMessage = "A descrição deve ter no máximo 255 caracteres")]
    public string Description { get; set; } = string.Empty;

    [EnumDataType(
        typeof(EVoucherDiscountType),
        ErrorMessage = "O tipo de desconto é inválido")]
    public EVoucherDiscountType DiscountType { get; set; }
        = EVoucherDiscountType.FixedAmount;

    [Range(
        0.01,
        double.MaxValue,
        ErrorMessage = "O valor deve ser maior que zero")]
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "O limite total de usos deve ser maior que zero")]
    public int? MaxTotalUses { get; set; }

    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "O limite por usuário deve ser maior que zero")]
    public int? MaxUsesPerUser { get; set; }

    [MaxLength(
        160,
        ErrorMessage = "O identificador do usuário deve ter no máximo 160 caracteres")]
    public string? AssignedUserId { get; set; }

    public long? ProductId { get; set; }

    public bool IsActive { get; set; } = true;
}