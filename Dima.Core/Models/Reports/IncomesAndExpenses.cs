using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models.Reports
{
    public record IncomesAndExpenses(string UserId, int Month, int Year, decimal Incomes, decimal Expenses)
    {
    }
}
