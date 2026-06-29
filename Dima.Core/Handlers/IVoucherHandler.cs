using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Handlers
{
    public interface IVoucherHandler
    {
        Task<Response<Voucher?>> GetByNumberAsync(GetVoucherByNumberRequest request);

    }
}
