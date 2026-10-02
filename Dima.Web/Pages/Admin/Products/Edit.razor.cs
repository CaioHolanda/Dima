using Dima.Core.Handlers;
using Dima.Core.Requests.Products;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Dima.Web.Pages.Admin.Products;

public partial class EditAdminProductPage : ComponentBase
{
    #region Properties

    [Parameter]
    public long Id { get; set; }

    public bool IsBusy { get; set; }

    public UpdateProductRequest InputModel { get; set; } = new();

    public CultureInfo BrazilCulture { get; } =
        CultureInfo.GetCultureInfo("pt-BR");
    public bool IsSaving { get; set; }
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

    public async Task OnValidSubmitAsync()
    {
        IsSaving = true;

        try
        {
            InputModel.Id = Id;

            var result = await Handler.UpdateAsync(InputModel);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ?? "Não foi possível atualizar o produto",
                    Severity.Error);

                return;
            }

            Snackbar.Add(
                result.Message ?? "Produto atualizado com sucesso",
                Severity.Success);

            NavigationManager.NavigateTo("/admin/produtos");
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            IsSaving = false;
        }
    }

    #endregion  
    
    #region Overrides

    protected override async Task OnParametersSetAsync()
    {
        IsBusy = true;

        try
        {
            var request = new GetProductByIdRequest
            {
                Id = Id
            };

            var result = await Handler.GetByIdForAdminAsync(request);

            if (!result.IsSuccess || result.Data is null)
            {
                Snackbar.Add(
                    result.Message ?? "[E129] Produto não encontrado",
                    Severity.Error);

                NavigationManager.NavigateTo("/admin/produtos");
                return;
            }

            InputModel = new UpdateProductRequest
            {
                Id =                    result.Data.Id,
                Title =                 result.Data.Title,
                Description =           result.Data.Description,
                Price =                 result.Data.Price,
                AccessDurationMonths =  result.Data.AccessDurationMonths,
                Slug =                  result.Data.Slug,
                IsActive =              result.Data.IsActive
            };
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            NavigationManager.NavigateTo("/admin/produtos");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}