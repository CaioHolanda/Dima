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
    public HashSet<long> ProductsBeingUpdated { get; set; } = [];

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
                   StringComparison.OrdinalIgnoreCase)
               || product.AccessDurationMonths
                   .ToString()
                   .Contains(
                      SearchTerm,
                      StringComparison.OrdinalIgnoreCase); 
    };

    public async Task OnStatusButtonClickedAsync(
    Product product)
    {
        var action = product.IsActive
            ? "desativado"
            : "reativado";

        var consequence = product.IsActive
            ? "e deixará de ser oferecido"
            : "e voltará a ser oferecido";

        var confirmed =
            await DialogService.ShowMessageBoxAsync(
                "ATENÇÃO",
                $"O produto \"{product.Title}\" será " +
                $"{action} {consequence}. Deseja continuar?",
                yesText: product.IsActive
                    ? "DESATIVAR"
                    : "REATIVAR",
                cancelText: "Cancelar");

        if (confirmed is not true)
            return;

        await OnStatusChangeAsync(product);
    }

    private async Task OnStatusChangeAsync(Product product)
    {
        var wasActive = product.IsActive;

        ProductsBeingUpdated.Add(product.Id);

        try
        {
            var result = wasActive
                ? await Handler.DeactivateAsync(
                    new DeactivateProductRequest
                    {
                        Id = product.Id
                    })
                : await Handler.ActivateAsync(
                    new ActivateProductRequest
                    {
                        Id = product.Id
                    });

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "Não foi possível alterar o estado do produto",
                    Severity.Error);

                return;
            }

            product.IsActive = !wasActive;

            Snackbar.Add(
                result.Message ??
                (wasActive
                    ? $"Produto {product.Title} desativado"
                    : $"Produto {product.Title} reativado"),
                Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            ProductsBeingUpdated.Remove(product.Id);
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
    public static string FormatAccessDuration(int months)
    {
        return months == 1
            ? "1 mês"
            : $"{months} meses";
    }

}