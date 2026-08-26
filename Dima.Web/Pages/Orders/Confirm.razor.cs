using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Orders
{
    public partial class ConfirmOrderPaymentPage : ComponentBase
    {
        #region Parameters
        [Parameter]
        public string Number { get; set; } = string.Empty;
        #endregion

        #region Properties
        public Order? Order { get; set; }
        public bool IsConfirming { get; set; } = true;
        public bool IsTimedOut { get; set; }
        #endregion

        #region Services
        [Inject] public IOrderHandler OrderHandler { get; set; } = null!;
        [Inject] public ISnackbar Snackbar { get; set; } = null!;

        #endregion

        #region Overrides

        protected override async Task OnInitializedAsync()
        {
            await CheckPaymentStatusAsync();
        }
        private async Task CheckPaymentStatusAsync()
        {
            IsConfirming = true;
            IsTimedOut = false;

            for (var attempt = 1; attempt <= 5; attempt++)
            {
                var request = new GetOrderByNumberRequest
                {
                    Number = Number
                };

                var result = await OrderHandler.GetByNumberAsync(request);

                if (!result.IsSuccess)
                {
                    Snackbar.Add(result.Message, Severity.Error);
                    IsConfirming = false;
                    return;
                }

                Order = result.Data;

                if (Order?.Status == EOrderStatus.Paid)
                {
                    IsConfirming = false;
                    return;
                }

                if (Order?.Status != EOrderStatus.WaintingPayment)
                {
                    IsConfirming = false;
                    return;
                }

                if (attempt < 5)
                    await Task.Delay(1000);
            }

            IsConfirming = false;
            IsTimedOut = true;
        }

        public async Task RetryAsync()
        {
            await CheckPaymentStatusAsync();
        }
        #endregion
    }
}
