using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Products;

public partial class ListAdminProductsPage
    : ComponentBase
{
    public bool IsBusy { get; set; }
    public List<Product> Products { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;

    [Inject]
    public IAdminProductHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    public Func<Product, bool> Filter => product =>
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return true;

        return product.Id.ToString().Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || product.Title.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || product.Slug.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || product.Description.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase);
    };

    public async Task OnDeactivateButtonClickedAsync(
        Product product)
    {
        var confirmed =
            await DialogService.ShowMessageBoxAsync(
                "ATENÇÃO",
                $"O produto \"{product.Title}\" será " +
                "desativado e deixará de ser oferecido. " +
                "Deseja continuar?",
                yesText: "DESATIVAR",
                cancelText: "Cancelar");

        if (confirmed is not true)
            return;

        await OnDeactivateAsync(product);
    }

    private async Task OnDeactivateAsync(Product product)
    {
        try
        {
            var result = await Handler.DeactivateAsync(
                new DeactivateProductRequest
                {
                    Id = product.Id
                });

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "Não foi possível desativar o produto",
                    Severity.Error);

                return;
            }

            product.IsActive = false;

            Snackbar.Add(
                result.Message ??
                $"Produto {product.Title} desativado",
                Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        IsBusy = true;

        try
        {
            var result =
                await Handler.GetAllForAdminAsync(
                    new GetAllAdminProductsRequest
                    {
                        PageNumber = 1,
                        PageSize = 100
                    });

            if (result.IsSuccess)
            {
                Products = result.Data ?? [];
                return;
            }

            Snackbar.Add(
                result.Message ??
                "Não foi possível carregar os produtos",
                Severity.Error);
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

}