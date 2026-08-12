using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Products;
using Dima.Core.Requests.Users;
using Dima.Core.Requests.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Vouchers;

public partial class EditAdminVoucherPage : ComponentBase
{
    #region Properties

    [Parameter]
    public long Id { get; set; }

    public bool IsBusy { get; set; }
    public bool IsSaving { get; set; }

    public List<Product> Products { get; set; } = [];

    public UpdateVoucherRequest InputModel { get; set; } = new();

    private UserLookup? _selectedUser;

    public UserLookup? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            InputModel.AssignedUserId = value?.Id;
        }
    }

    #endregion

    #region Services

    [Inject]
    public IAdminVoucherHandler Handler { get; set; } = null!;

    [Inject]
    public IAdminProductHandler ProductHandler { get; set; } = null!;

    [Inject]
    public IAdminUserHandler UserHandler { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    #endregion

    #region Methods

    public async Task<IEnumerable<UserLookup>>
        SearchUsersAsync(
            string searchTerm,
            CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) ||
            searchTerm.Trim().Length < 2)
        {
            return [];
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await UserHandler.SearchAsync(
                new SearchUsersRequest
                {
                    SearchTerm = searchTerm.Trim(),
                    Limit = 10
                });

            cancellationToken.ThrowIfCancellationRequested();

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "[E162] Não foi possível pesquisar os usuários",
                    Severity.Error);

                return [];
            }

            return result.Data ?? [];
        }
        catch (OperationCanceledException)
        {
            return [];
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
            return [];
        }
    }

    public async Task OnValidSubmitAsync()
    {
        if (IsSaving)
            return;

        IsSaving = true;

        try
        {
            InputModel.Id = Id;

            var result =
                await Handler.UpdateAsync(InputModel);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "[E163] Não foi possível atualizar o voucher",
                    Severity.Error);

                return;
            }

            Snackbar.Add(
                result.Message ??
                "Voucher atualizado com sucesso",
                Severity.Success);

            NavigationManager.NavigateTo(
                "/admin/vouchers");
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                ex.Message,
                Severity.Error);
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
            var voucherTask =
                Handler.GetByIdForAdminAsync(
                    new GetVoucherByIdRequest
                    {
                        Id = Id
                    });

            var productsTask =
                ProductHandler.GetAllForAdminAsync(
                    new GetAllAdminProductsRequest
                    {
                        PageNumber = 1,
                        PageSize = 100
                    });

            await Task.WhenAll(
                voucherTask,
                productsTask);

            var voucherResult = await voucherTask;
            var productsResult = await productsTask;

            if (!voucherResult.IsSuccess ||
                voucherResult.Data is null)
            {
                Snackbar.Add(
                    voucherResult.Message ??
                    "[E164] Voucher não encontrado",
                    Severity.Error);

                NavigationManager.NavigateTo(
                    "/admin/vouchers");

                return;
            }

            if (!productsResult.IsSuccess)
            {
                Snackbar.Add(
                    productsResult.Message ??
                    "[E165] Não foi possível carregar os produtos",
                    Severity.Error);

                NavigationManager.NavigateTo(
                    "/admin/vouchers");

                return;
            }

            Products = productsResult.Data?
                .OrderBy(x => x.Title)
                .ToList() ?? [];

            var voucher = voucherResult.Data;

            InputModel = new UpdateVoucherRequest
            {
                Id = voucher.Id,
                Code = voucher.Code,
                Title = voucher.Title,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                Value = voucher.Value,
                StartsAt = voucher.StartsAt,
                EndsAt = voucher.EndsAt,
                MaxTotalUses = voucher.MaxTotalUses,
                MaxUsesPerUser = voucher.MaxUsesPerUser,
                AssignedUserId = voucher.AssignedUserId,
                ProductId = voucher.ProductId,
                IsActive = voucher.IsActive
            };

            _selectedUser =
                voucher.AssignedUserId.HasValue
                    ? new UserLookup
                    {
                        Id = voucher.AssignedUserId.Value,
                        Email =
                            voucher.AssignedUserEmail ??
                            $"Usuário #{voucher.AssignedUserId.Value}"
                    }
                    : null;
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                ex.Message,
                Severity.Error);

            NavigationManager.NavigateTo(
                "/admin/vouchers");
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}