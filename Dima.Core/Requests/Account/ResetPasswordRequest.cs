using System.ComponentModel.DataAnnotations;

namespace Dima.Core.Requests.Account;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Código de redefinição obrigatório")]
    public string ResetCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova senha obrigatória")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmação obrigatória")]
    [Compare(
        nameof(NewPassword),
        ErrorMessage = "As senhas não coincidem")]
    public string ConfirmPassword { get; set; } = string.Empty;
}