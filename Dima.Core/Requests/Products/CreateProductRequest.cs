using System.ComponentModel.DataAnnotations;

namespace Dima.Core.Requests.Products;

public class CreateProductRequest
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [MaxLength(80, ErrorMessage = "O título deve ter no máximo 80 caracteres")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória")]
    [MaxLength(255, ErrorMessage = "A descrição deve ter no máximo 255 caracteres")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "O slug é obrigatório")]
    [MaxLength(80, ErrorMessage = "O slug deve ter no máximo 80 caracteres")]
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}