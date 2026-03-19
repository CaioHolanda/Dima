using Dima.Core.Requests.Account;
using Dima.Core.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Handlers
{
    public interface IAccountHandler
    {
        Task<Response<string>> LoginAsync(LoginRequest request);
        Task<Response<string>> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();

    }
}
