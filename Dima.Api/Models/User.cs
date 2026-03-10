using Microsoft.AspNetCore.Identity;

namespace Dima.Api.Models
{
    // Definicao da classe de usuario que contera uma lista dos Roles
    public class User:IdentityUser<long>
    {
        public List<IdentityRole<long>>? Roles { get; set; }
    }
}
