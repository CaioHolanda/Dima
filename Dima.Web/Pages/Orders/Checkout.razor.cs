using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Dima.Core.Requests.Vouchers;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components.Web;

namespace Dima.Web.Pages.Orders
{
    public partial class CheckoutoutPage :ComponentBase
    {
        #region Parameters
        [Parameter] public string ProductSlug { get; set; } = string.Empty;
        [SupplyParameterFromQuery(Name ="voucher")] public string? VoucherCode { get; set; }
        #endregion

        #region Properties
        public bool IsBusy { get; set; }
        public bool IsValid { get; set; }
        public Product? Product { get; set; }
        public VoucherApplication? AppliedVoucher { get; set; }
        public bool IsApplyingVoucher { get; set; }
        public decimal DiscountAmount =>
            AppliedVoucher?.DiscountAmount ?? 0m;

        public decimal Total =>
            AppliedVoucher?.Total ??
            Product?.Price ??
            0m;
        #endregion

        #region Services
        [Inject] public IProductHandler ProductHandler { get; set; } = null!;
        [Inject] public IOrderHandler OrderHandler { get; set; } = null!;
        [Inject] public IVoucherHandler VoucherHandler { get; set; } = null!;
        [Inject] public NavigationManager NavigationManager { get; set; } = null!;
        [Inject] public ISnackbar Snackbar { get; set; } = null!;
        #endregion

        #region Methods
        public async Task ApplyVoucherAsync(
                bool showSuccessMessage = true)
        {
            if (Product is null ||
                string.IsNullOrWhiteSpace(VoucherCode) ||
                IsApplyingVoucher)
            {
                return;
            }

            IsApplyingVoucher = true;
            AppliedVoucher = null;

            try
            {
                var result = await VoucherHandler.ApplyAsync(
                    new ApplyVoucherRequest
                    {
                        Code = VoucherCode,
                        ProductId = Product.Id
                    });

                if (!result.IsSuccess || result.Data is null)
                {
                    Snackbar.Add(
                        result.Message,
                        Severity.Warning);

                    return;
                }

                AppliedVoucher = result.Data;
                VoucherCode = result.Data.Code;

                if (showSuccessMessage)
                {
                    Snackbar.Add(
                        result.Message,
                        Severity.Success);
                }
            }
            catch
            {
                Snackbar.Add(
                    "[E236] Não foi possível aplicar o voucher",
                    Severity.Error);
            }
            finally
            {
                IsApplyingVoucher = false;
            }
        }

        public async Task OnVoucherAdornmentClickAsync()
        {
            if (AppliedVoucher is not null)
            {
                RemoveVoucher();
                return;
            }

            await ApplyVoucherAsync();
        }

        public async Task OnVoucherKeyDownAsync(
            KeyboardEventArgs args)
        {
            if (args.Key == "Enter" &&
                AppliedVoucher is null)
            {
                await ApplyVoucherAsync();
            }
        }

        public void RemoveVoucher()
        {
            AppliedVoucher = null;
            VoucherCode = string.Empty;
        }
        protected override async Task OnInitializedAsync()
        {
            IsValid = false;

            // Recupera o produto
            try
            {
                var result = await ProductHandler.GetBySlugAsync(
                    new GetProductBySlugRequest
                    {
                        Slug = ProductSlug
                    });

                if (!result.IsSuccess || result.Data is null)
                {
                    Snackbar.Add(
                        "[E073] Não foi possível obter o produto",
                        Severity.Error);

                    return;
                }

                Product = result.Data;
            }
            catch
            {
                Snackbar.Add(
                    "[E074] Não foi possível obter o produto",
                    Severity.Error);

                return;
            }

            // Aplica o voucher recebido pela query string
            if (!string.IsNullOrWhiteSpace(VoucherCode))
            {
                await ApplyVoucherAsync(showSuccessMessage: false);
            }


            IsValid = true;
        }
        public async Task OnValidSubmitAsync()
        {
            if (!IsValid || Product is null)
            {
                Snackbar.Add(
                    "[E132] Não foi possível identificar o produto.",
                    Severity.Error);

                return;
            }

            IsBusy = true;

            try
            {
                var request = new CreateOrderRequest
                {
                    ProductId = Product.Id,
                    VoucherId = AppliedVoucher?.VoucherId
                };

                var result = await OrderHandler.CreateAsync(request);

                if (result.IsSuccess && result.Data is not null)
                {
                    NavigationManager.NavigateTo(
                        $"/pedidos/{result.Data.Number}");
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
    }
}
