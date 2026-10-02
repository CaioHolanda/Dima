using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Users;

public class GetAllAdminUsersRequest : AdminPagedRequest
{
    public bool? IsPremium { get; set; }
}