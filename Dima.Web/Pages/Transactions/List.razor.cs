using Dima.Core.Common.Extensions;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Transactions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Transactions
{
    public partial class ListTransactionsPage:ComponentBase
    {
        #region Properties

        public bool IsBusy { get; set; } = false;
        public List<Transaction> Transactions { get; set; } = [];
        public string SearchTerm { get; set; } = string.Empty;
        public int CurrentYear { get; set; } = DateTime.Now.Year;
        public int CurrentMonth { get; set; } = DateTime.Now.Month;
        public int[] Years { get; set; } =
        {
            DateTime.Now.Year,
            DateTime.Now.AddYears(-1).Year,
            DateTime.Now.AddYears(-2).Year,
            DateTime.Now.AddYears(-3).Year
        };

        #endregion

        #region Services

        [Inject]
        public ISnackbar Snackbar { get; set; } = null!;
        [Inject]
        public IDialogService DialogService { get; set; } = null!;
        [Inject]
        public ITransactionHandler Handler { get; set; } = null!;

        #endregion

        #region Private_Methods

        public async Task OnSearchAsync()
        {
            await GetTransactionsAsync();
            StateHasChanged();
        }

        private async Task GetTransactionsAsync()
        {
            IsBusy = true;
            try
            {
                var request = new GetTransactionsByPeriodRequest
                {
                    StartDate = DateTime.Now.GetFirstDay(CurrentYear, CurrentMonth),
                    EndDate = DateTime.Now.GetLastDay(CurrentYear, CurrentMonth),
                    PageNumber = 1,
                    PageSize = 1000
                };
                var result = await Handler.GetByPeriodAsync(request);
                if (result.IsSuccess)
                    Transactions = result.Data ?? [];
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task OnDeleteAsync(long id, string title)
        {
            IsBusy = true;
            try
            {
                var result = await Handler.DeleteAsync(new DeleteTransactionRequest { Id = id });
                if (result.IsSuccess)
                {
                    Snackbar.Add($"Lancamento {title} removido!", Severity.Success);
                    Transactions.RemoveAll(x => x.Id == id);
                }
                else
                {
                    Snackbar.Add(result.Message, Severity.Error);

                }
            }
            catch (Exception ex)
            {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Overrides

        protected override async Task OnInitializedAsync()
        => await GetTransactionsAsync();

        #endregion

        #region Public_Methods

        public Func<Transaction, bool> Filter => transaction =>
        {
            if (string.IsNullOrEmpty(SearchTerm))
                return true;
            if (transaction.Id.ToString().Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                return true;
            if (transaction.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        };

        public async void OnDeleteButtonClickedAsync(long id,string title)
        {
            var result = await DialogService.ShowMessageBoxAsync(
                "ATENCAO",
                $"Lancamento {title} sera excluido. Continua?",
                yesText:"EXCLUIR",
                cancelText:"Cancelar");
            if (result is true)
                await OnDeleteAsync(id, title);
            StateHasChanged();
        }

        #endregion
    }
}
