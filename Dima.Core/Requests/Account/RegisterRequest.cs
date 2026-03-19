using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dima.Core.Requests.Account
{
    public class RegisterRequest:Request
    {
        [Required(ErrorMessage = "Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Invalid Password")]
        public string Password { get; set; } = string.Empty;
    }
}
