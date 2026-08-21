using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;

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
        public Voucher? Voucher { get; set; }
        public decimal Total { get; set; }
        #endregion

        #region Services
        [Inject] public IProductHandler ProductHandler { get; set; } = null!;
        [Inject] public IOrderHandler OrderHandler { get; set; } = null!;
        [Inject] public IVoucherHandler VoucherHandler { get; set; } = null!;
        [Inject] public NavigationManager NavigationManager { get; set; } = null!;
        [Inject] public ISnackbar Snackbar { get; set; } = null!;
        #endregion

        #region Methods

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

            // Recupera o voucher, quando informado
            if (!string.IsNullOrWhiteSpace(VoucherCode))
            {
                try
                {
                    var result = await VoucherHandler.GetByCodeAsync(
                        new GetVoucherByCodeRequest
                        {
                            Code = VoucherCode
                        });

                    if (!result.IsSuccess || result.Data is null)
                    {
                        Voucher = null;
                        VoucherCode = string.Empty;

                        Snackbar.Add(
                            "[E076] Não foi possível obter o voucher",
                            Severity.Warning);
                    }
                    else
                    {
                        Voucher = result.Data;
                    }
                }
                catch
                {
                    Voucher = null;
                    VoucherCode = string.Empty;

                    Snackbar.Add(
                        "[E078] Não foi possível obter o voucher",
                        Severity.Warning);
                }
            }

            var discount = CalculateDiscount(Product.Price, Voucher);
            Total = Product.Price - discount;

            IsValid = true;
        }
        protected static decimal CalculateDiscount(
            decimal price,
            Voucher? voucher)
        {
            if (voucher is null)
                return 0;

            var discount = voucher.DiscountType switch
            {
                EVoucherDiscountType.FixedAmount => voucher.Value,

                EVoucherDiscountType.Percentage =>
                    price * voucher.Value / 100,

                _ => 0
            };

            return Math.Min(price, discount);
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
                    VoucherId = Voucher?.Id
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
