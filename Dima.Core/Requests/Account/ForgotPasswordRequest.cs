using System.ComponentModel.DataAnnotations;

namespace Dima.Core.Requests.Account;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "E-mail obrigatório")]
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    public string Email { get; set; } = string.Empty;
}