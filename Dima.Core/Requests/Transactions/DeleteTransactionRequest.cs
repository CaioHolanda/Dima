using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Requests.Transactions;

public class DeleteTransactionRequest:Request
{
    public long Id { get; set; }
}

