using System;
using System.Collections.Generic;
using System.Text;

namespace Dima.Core.Models.Reports
{
    public record ExpensesByCategory(string UserId, string Category, int Year, decimal Expenses)
    {


    }
}
