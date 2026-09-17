using Dima.Api.Observability;
using Microsoft.Extensions.Logging.Abstractions;
using Dima.Api.Data;
using Dima.Api.Services;
using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Reports;
using Dima.Core.Requests.Reports;
using Dima.Core.Responses;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Handlers
{
    public class ReportHandler(AppDbContext context, BusinessTime businessTime, ILogger<ReportHandler>? logger = null) : IReportHandler
    {
        private readonly ILogger<ReportHandler> _logger = logger ?? NullLogger<ReportHandler>.Instance;

        public async Task<Response<List<ExpensesByCategory>?>> GetExpensesByCategoryReportAsync(GetExpensesByCategoryRequest request)
        {
            try
            {
                var data = await context
                                .ExpensesByCategory
                                .AsNoTracking()
                                .Where(x => x.UserId == request.UserId)
                                .OrderByDescending(x => x.Year)
                                .ThenBy(x => x.Category)
                                .ToListAsync();
                return new Response<List<ExpensesByCategory>?>(data);
            }
            catch (Exception exception)
            {
                _logger.LogOperationError(exception);

                return new Response<List<ExpensesByCategory>?>(null, 500, "Nao foi possivel coletar os dados: Expenses by Categoria.");
            }
        }
        // Financial Summary e' apenas um objeto que entrega o Saldo = (Incomes - Expenses)
        public async Task<Response<FinancialSummary?>> GetFinancialSummaryReportAsync(GetFinancialSummaryRequest request)
        {
            // Faz o resumo financeiro do mes corrente, comecando do dia 01
            var now = businessTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            try
            {
                var data = await context
                .Transactions
                .AsNoTracking()
                .Where(
                    x => x.UserId == request.UserId &&
                        x.PaidOrReceivedAt >= startDate &&
                        x.PaidOrReceivedAt <= now)
                .GroupBy(x => 1) // sempre verdadeiro, nao depende de informacao em comum
                .Select(
                    x => new FinancialSummary(
                        request.UserId,
                        x.Where(ty => ty.Type == ETransactionType.Deposit).Sum(t => t.Amount),  // Incomes
                        x.Where(ty => ty.Type == ETransactionType.Withdraw).Sum(t => t.Amount)   // Expenses
                        ))
                .FirstOrDefaultAsync();
                return new Response<FinancialSummary?>(data);
            }
            catch (Exception exception)
            {
                _logger.LogOperationError(exception);

                return new Response<FinancialSummary?> (null, 500, "Nao foi possivel calcular o saldo.");
            }

        }

        public async Task<Response<List<IncomesAndExpenses>?>> GetIncomesAndExpensesReportAsync(GetIncomesAndExpensesRequest request)
        {
            try
            {
                var data = await context
                                .IncomesAndExpenses
                                .AsNoTracking()
                                .Where(x => x.UserId == request.UserId)
                                .OrderByDescending(x => x.Year)
                                .ThenBy(x => x.Month)
                                .ToListAsync();
                return new Response<List<IncomesAndExpenses>?>(data);
            }
            catch (Exception exception)
            {
                _logger.LogOperationError(exception);

                return new Response<List<IncomesAndExpenses>?>(null, 500, "Nao foi possivel coletar os dados: Incomes and Expenses.");
            }
        }

        public async Task<Response<List<IncomesByCategory>?>> GetIncomesByCategoryReportAsync(GetIncomesByCategoryRequest request)
        {
            try
            {
                var data = await context
                                .IncomesByCategory
                                .AsNoTracking()
                                .Where(x => x.UserId == request.UserId)
                                .OrderByDescending(x => x.Year)
                                .ThenBy(x => x.Category)
                                .ToListAsync();
                return new Response<List<IncomesByCategory>?>(data);
            }
            catch (Exception exception)
            {
                _logger.LogOperationError(exception);

                return new Response<List<IncomesByCategory>?>(null, 500, "Nao foi possivel coletar os dados: Incomes by Categoria.");
            }
        }
    }
}
