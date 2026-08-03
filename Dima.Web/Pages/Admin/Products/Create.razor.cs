using Dima.Core.Handlers;
using Dima.Core.Requests.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dima.Web.Pages.Admin.Products;

public partial class CreateAdminProductPage : ComponentBase
{
    #region Properties
    public CultureInfo BrazilCulture { get; } =
    CultureInfo.GetCultureInfo("pt-BR");
    public bool IsBusy { get; set; }

    public CreateProductRequest InputModel { get; set; } = new()
    {
        IsActive = true
    };

    #endregion

    #region Services

    [Inject]
    public IAdminProductHandler Handler { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    #endregion

    #region Methods
    private static readonly Regex SlugPattern = new(
    "^[a-z0-9]+(?:-[a-z0-9]+)*$",
    RegexOptions.Compiled);

    public string? ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "Campo obrigatório";

        if (!SlugPattern.IsMatch(slug))
            return "Use apenas letras minúsculas, números e hífens";

        return null;
    }
    public async Task OnValidSubmitAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            var result = await Handler.CreateAsync(InputModel);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ?? "[E125] Não foi possível criar o produto",
                    Severity.Error);

                return;
            }

            Snackbar.Add(
                result.Message ?? "Produto criado com sucesso",
                Severity.Success);

            NavigationManager.NavigateTo("/admin/produtos");
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