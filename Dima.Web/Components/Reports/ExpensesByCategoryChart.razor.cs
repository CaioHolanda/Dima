using Dima.Core.Handlers;
using Dima.Core.Requests.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Components.Reports
{
    public partial class ExpensesByCategoryChartComponent : ComponentBase
    {
        #region Properties
        public List<ChartSeries<double>> Series { get; set; } = [];
        public string[] Labels { get; set; } = [];
        #endregion

        #region Services

        [Inject]
        public IReportHandler Handler { get; set; } = null!;
        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;

        #endregion

        #region Overrides

        protected override async Task OnInitializedAsync()
        {
            await GetExpensesByCategoryAsync();
        }
        private async Task GetExpensesByCategoryAsync()
        {
            var labels = new List<string>();
            var values = new List<double>();
            var request = new GetExpensesByCategoryRequest();
            var result = await Handler.GetExpensesByCategoryReportAsync(request);
            if (!result.IsSuccess || result.Data is null)
            {
                Snackbar.Add("[E032] Falha a obter dados do relatorio.", Severity.Error);
                return;
            }
            foreach (var item in result.Data)
            {
                var value = Math.Abs((double)item.Expenses);

                labels.Add($"{item.Category} ({value:C2})");
                values.Add(value);
            }

            Labels = labels.ToArray();

            Series =
            [
                new ChartSeries<double>
                {
                    Name = "Despesas",
                    Data = new ChartData<double>(values.ToArray())
                }
            ];
        }

        #endregion
    }
}
