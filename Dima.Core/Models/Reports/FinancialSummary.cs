using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models.Reports
{
    public record FinancialSummary(string UserId,decimal Incomes, decimal Expenses)
    {
        public decimal Total => Incomes - (Expenses < 0 ? -Expenses : Expenses);

    }
}
