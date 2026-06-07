using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dima.Core.Requests.Account
{
    public class LoginRequest : Request
    {
        [Required(ErrorMessage = "Email")]
        [EmailAddress(ErrorMessage = "Email Invalido")]
        public string Email { get; set; } = string.Empty;


        [Required(ErrorMessage = "Senha Invalida")]
        public string Password { get; set; } = string.Empty;
    }
}
