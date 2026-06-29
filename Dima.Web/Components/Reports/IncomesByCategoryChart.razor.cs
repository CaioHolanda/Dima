using Dima.Core.Handlers;
using Dima.Core.Requests.Reports;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Components.Reports
{
    public partial class IncomesByCategoryChartComponent : ComponentBase
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
            await GetIncomesByCategoryAsync();
        }
        private async Task GetIncomesByCategoryAsync()
        {
            var labels = new List<string>();
            var values = new List<double>();
            var request = new GetIncomesByCategoryRequest();
            var result = await Handler.GetIncomesByCategoryReportAsync(request);
            if (!result.IsSuccess || result.Data is null)
            {
                Snackbar.Add("[E033] Falha a obter dados do relatorio.", Severity.Error);
                return;
            }
            foreach (var item in result.Data)
            {
                var value = Math.Abs((double)item.Incomes);

                labels.Add($"{item.Category} ({value:C2})");
                values.Add(value);
            }
            Labels = labels.ToArray();

            Series =
            [
                new ChartSeries<double>
                {
                    Name = "Entradas",
                    Data = new ChartData<double>(values.ToArray())
                }
            ];
        }

        #endregion
    }
}
