using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models.Reports
{
    public record IncomesByCategory(string UserId,string Category, int Year, decimal Incomes)
    {

    }
}
