using Dima.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dima.Core.Requests.Transactions;

public class UpdateTransactionRequest:Request
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Invalid Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Invalid Type")]
    public ETransactionType Type { get; set; } = ETransactionType.Withdraw;

    [Required(ErrorMessage = "Invalid amount")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Invalid Category")]
    public long CategoryId { get; set; }

    [Required(ErrorMessage = "Invalid Date")]
    public DateTime? PaidOrReceivedAt { get; set; }
}

