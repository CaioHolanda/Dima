using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Transactions;

public class GetTransactionByIdRequest:Request
{
    public long Id { get; set; }
}
