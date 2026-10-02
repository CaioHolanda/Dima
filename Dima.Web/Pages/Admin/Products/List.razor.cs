using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Products;

public partial class ListAdminProductsPage
    : Dima.Web.Pages.Admin.AdminListPage<Product>
{
    public List<Product> Products { get; set; } = [];
    public HashSet<long> ProductsBeingUpdated { get; set; } = [];

    [Inject]
    public IAdminProductHandler Handler { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

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
            await ReloadAsync();

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

    protected override async Task<Dima.Core.Responses.PagedResponse<List<Product>?>> FetchAsync()
    {
        var request = new GetAllAdminProductsRequest();
        Configure(request);
        return await Handler.GetAllForAdminAsync(request);
    }
    protected override void SetItems(List<Product> items) => Products = items;

    public static string FormatAccessDuration(int months)
    {
        return months == 1
            ? "1 mês"
            : $"{months} meses";
    }

}