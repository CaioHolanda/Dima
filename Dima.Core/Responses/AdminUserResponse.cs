using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Responses
{
    public class AdminUserResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Plan { get; set; } = string.Empty;
        public string? ProductName { get; set; }

        public bool IsActive { get; set; }
    }
}
