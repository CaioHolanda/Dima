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
        [SupplyParameterFromQuery(Name ="voucher")] public string? VoucherNumber { get; set; }
        #endregion

        #region Properties
        public PatternMask Mask = new("####-####") 
        {
            MaskChars = [new MaskChar('#',@"[0-9a-fA-F]")],
            Placeholder = '_',
            CleanDelimiters = true,
            Transformation = AllUpperCase
        };
        public bool IsBusy { get; set; }
        public bool IsValid { get; set; }
        public CreateOrderRequest InputModel { get; set; } = new();
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
        private static char AllUpperCase(char c) => c.ToString().ToUpperInvariant()[0];

        protected override async Task OnInitializedAsync()
        {
            // Passos: Recuperar o Produto e depois o Voucher
            try
            {
                var result = await ProductHandler.GetBySlugAsync(new GetProductBySlugRequest
                {
                    Slug = ProductSlug
                });
                if (result.IsSuccess == false)
                {
                    Snackbar.Add("[E073] Nao foi possivel obter o produto", Severity.Error);
                    IsValid = false;
                    return;
                }
                Product = result.Data;
            }
            catch 
            {
                Snackbar.Add("[E074] Nao foi possivel obter o produto", Severity.Error);
                IsValid = false;
                return;
            }
            if(Product is null)
            {
                Snackbar.Add("[E075] Nao foi possivel obter o produto", Severity.Error);
                IsValid = false;
                return;
            }
            if (string.IsNullOrEmpty(VoucherNumber)==false) // So busca o Voucher se o mesmo for informado
            {
                try
                {
                    var result = await VoucherHandler.GetByNumberAsync(new GetVoucherByNumberRequest
                    {
                        Number=VoucherNumber.Replace("-","")
                    });
                    if(result.IsSuccess==false)
                    {
                        VoucherNumber = string.Empty;
                        Snackbar.Add("[E076] Nao foi possivel obter o Voucher");
                    }
                    if(result.Data is null)
                    {
                        VoucherNumber = string.Empty;
                        Snackbar.Add("[E077] Nao foi possivel obter o Voucher");
                    }
                    Voucher = result.Data;
                }
                catch 
                {
                    VoucherNumber = string.Empty;
                    Snackbar.Add("[E078] Nao foi possivel obter o Voucher");
                }
            }
            IsValid = true;
            Total = Product.Price - (Voucher?.Amount ?? 0);
        }
        #endregion
    }
}
