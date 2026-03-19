using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models
{
    public class User
    {
        public string Email { get; set; } = string.Empty;
        public bool IsMailConfirmed { get; set; }
        public Dictionary<string, string> Claims { get; set; } = [];
    }
}
