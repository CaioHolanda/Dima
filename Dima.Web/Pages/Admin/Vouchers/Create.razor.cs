using Dima.Core.Handlers;
using Dima.Core.Requests.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Users;

namespace Dima.Web.Pages.Admin.Vouchers;

public partial class CreateAdminVoucherPage : ComponentBase
{
    #region Properties

    public bool IsBusy { get; set; }
    public CreateVoucherRequest InputModel { get; set; } = new()
    {
        IsActive = true
    };
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
    public IAdminUserHandler UserHandler { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    #endregion

    #region Methods
    public async Task<IEnumerable<UserLookup>> SearchUsersAsync(
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
            var result = await UserHandler.SearchAsync(
                new SearchUsersRequest
                {
                    SearchTerm = searchTerm.Trim(),
                    Limit = 10
                });

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "[E161] Não foi possível pesquisar os usuários",
                    Severity.Error);

                return [];
            }

            cancellationToken.ThrowIfCancellationRequested();

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
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            var result = await Handler.CreateAsync(InputModel);

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "[E157] Não foi possível criar o voucher",
                    Severity.Error);

                return;
            }

            Snackbar.Add(
                result.Message ??
                "Voucher criado com sucesso",
                Severity.Success);

            NavigationManager.NavigateTo("/admin/vouchers");
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                ex.Message,
                Severity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}