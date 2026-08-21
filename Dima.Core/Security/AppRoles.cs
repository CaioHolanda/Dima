using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Security
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static readonly string[] All =
        [
            Admin,
            User
        ];
    }
}
