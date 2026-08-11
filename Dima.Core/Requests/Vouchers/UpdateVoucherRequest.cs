using Dima.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Dima.Core.Requests.Vouchers;

public class UpdateVoucherRequest
{
    [JsonIgnore]
    public long Id { get; set; }

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

    [Range(
        0.01,
        double.MaxValue,
        ErrorMessage = "O valor deve ser maior que zero")]
    public decimal Value { get; set; }

    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxTotalUses { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxUsesPerUser { get; set; }

    [MaxLength(160)]
    public string? AssignedUserId { get; set; }

    public long? ProductId { get; set; }

    public bool IsActive { get; set; }
}