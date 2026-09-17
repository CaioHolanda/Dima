using Dima.Core.Handlers;
using Dima.Core.Models.Reports;
using Dima.Core.Requests.Reports;
using Dima.Core.Responses;
using System.Net.Http.Json;

namespace Dima.Web.Handlers
{
    public class ReportHandler(IHttpClientFactory httpClientFactory) : IReportHandler
    {
        private readonly HttpClient _client=httpClientFactory.CreateClient(Configuration.HttpClientName);
        public async Task<Response<List<ExpensesByCategory>?>> GetExpensesByCategoryReportAsync(GetExpensesByCategoryRequest request)
        {
            return await _client.GetResponseAsync<List<ExpensesByCategory>?>($"v1/reports/expenses", "[E028] Nao foi possivel obter dados");
        }

        public async Task<Response<FinancialSummary?>> GetFinancialSummaryReportAsync(GetFinancialSummaryRequest request)
        {
            return await _client.GetResponseAsync<FinancialSummary?>($"v1/reports/summary", "[E029] Nao foi possivel obter dados");
        }

        public async Task<Response<List<IncomesAndExpenses>?>> GetIncomesAndExpensesReportAsync(GetIncomesAndExpensesRequest request)
        {
            return await _client.GetResponseAsync<List<IncomesAndExpenses>?>($"v1/reports/incomes-expenses", "[E030] Nao foi possivel obter dados");
        }

        public async Task<Response<List<IncomesByCategory>?>> GetIncomesByCategoryReportAsync(GetIncomesByCategoryRequest request)
        {
            return await _client.GetResponseAsync<List<IncomesByCategory>?>($"v1/reports/incomes", "[E031] Nao foi possivel obter dados");
        }
    }
}
