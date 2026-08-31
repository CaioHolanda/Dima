using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Payment;
using Dima.Web.Pages.Orders;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dima.Web.Components.Orders
{
    public partial class OrderActionComponent:ComponentBase
    {
        #region Properties
        public bool CanRequestRefund
        {
            get
            {
                if (Order.Status != EOrderStatus.Paid)
                    return false;

                if (Order.AccessStartsAt is null)
                    return false;

                var now = DateTime.Now;

                // Plano futuro ainda não iniciado:
                // refund integral permitido.
                if (Order.AccessStartsAt.Value > now)
                    return true;

                // Plano já iniciado:
                // precisa ter data de pagamento e estar dentro dos 14 dias.
                if (Order.PaidAt is null)
                    return false;

                return now <= Order.PaidAt.Value.AddDays(14);
            }
        }
        #endregion

        #region Parameters
        [CascadingParameter] public DetailsPage Parent { get; set; } = null!;

        [Parameter,EditorRequired]
        public Order Order { get; set; } = null!;

        #endregion

        #region Services

        [Inject] public IDialogService DialogService { get; set; } = null!;
        [Inject] public IJSRuntime JsRuntime { get; set; } = null!;
        [Inject] public IOrderHandler OrderHandler { get; set; } = null!;
        [Inject] public IPaymentHandler PaymentHandler { get; set; } = null!;
        [Inject] public ISnackbar Snackbar { get; set; } = null!;

        #endregion

        #region Public Methods

        public async void OnCancelButtonClicked()
        {
            bool? result = await DialogService.ShowMessageBoxAsync("ATENCAO", 
                                                "Confirma o cancelamento?",
                                                yesText:"SIM",
                                                cancelText:"NAO");
            if (result is not null && result == true)
                await CancelOrderAsync();

        }

        public async void OnPayButtonClickedAsync()
        {
            await PayOrderAsync();
        }

        public async void OnRefundButtonClicked()
        {
            var options = new DialogOptions
            {
                CloseButton = true,
                MaxWidth = MaxWidth.Small,
                FullWidth = true
            };

            var dialog = await DialogService.ShowAsync<RefundDialog>(
                "Solicitar reembolso",
                options);

            var result = await dialog.Result;

            if (result is null || result.Canceled)
                return;

            if (result.Data is not RefundDialogResult refundData)
                return;

            await RefundOrderAsync(
                refundData.Reason,
                refundData.Details);
        }
        #endregion

        #region Private Methods

        private async Task CancelOrderAsync()
        {
            var request = new CancelOrderRequest
            {
                Id = Order.Id
            };
            var result = await OrderHandler.CancelAsync(request);
            if (result.IsSuccess)
                Parent.RefreshState(result.Data!);
            else
                Snackbar.Add(result.Message, Severity.Error);
        }

        private async Task PayOrderAsync()
        {
            var request = new CreatePaymentSessionRequest
            {
                OrderNumber = Order.Number
            };
            try
            {
                var result = await PaymentHandler.CreateSessionAsync(request);
                if (result.IsSuccess == false)
                {
                    Snackbar.Add(result.Message, Severity.Error);
                    return;
                }
                if (result.Data is null)
                {
                    Snackbar.Add(result.Message, Severity.Error);
                    return;
                }
                await JsRuntime.InvokeVoidAsync(
                    "checkout",
                    result.Data);
            }
            catch (JSException ex)
            {
                Snackbar.Add($"[JS] {ex.Message}", Severity.Error);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"[E081] {ex.Message}", Severity.Error);
            }
        }

        private async Task RefundOrderAsync(
            ERefundReason reason,
            string? details)
        {
            var request = new RefundOrderRequest
            {
                Id = Order.Id,
                RefundReason = reason,
                RefundReasonDetails = details
            };

            var result = await OrderHandler.RefundAsync(request);

            if (result.IsSuccess)
                Parent.RefreshState(result.Data!);
            else
                Snackbar.Add(result.Message, Severity.Error);
        }
        #endregion

    }
}
