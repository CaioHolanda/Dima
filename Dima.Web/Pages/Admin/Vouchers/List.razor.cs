using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Vouchers;

public partial class ListAdminVouchersPage
    : ComponentBase
{
    public bool IsBusy { get; set; }
    public List<AdminVoucherListItem> Vouchers { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;
    public HashSet<long> VouchersBeingUpdated { get; set; } = [];

    [Inject]
    public IAdminVoucherHandler Handler { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    public Func<AdminVoucherListItem, bool> Filter =>
        voucher =>
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return true;

            var assignedUser =
            voucher.AssignedUserEmail ?? "Todos";

            return voucher.Id.ToString().Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || voucher.Code.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || voucher.Title.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || voucher.Description.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase)
               || assignedUser.Contains(
                   SearchTerm,
                   StringComparison.OrdinalIgnoreCase);
        };

    protected override async Task OnInitializedAsync()
    {
        IsBusy = true;

        try
        {
            var result =
                await Handler.GetAllForAdminAsync(
                    new GetAllAdminVouchersRequest
                    {
                        PageNumber = 1,
                        PageSize = 100
                    });

            if (result.IsSuccess)
            {
                Vouchers = result.Data ?? [];
                return;
            }

            Snackbar.Add(
                result.Message ??
                "Não foi possível carregar os vouchers",
                Severity.Error);
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
    public async Task OnStatusButtonClickedAsync(AdminVoucherListItem voucher)
    {
        var action = voucher.IsActive
            ? "desativado"
            : "reativado";

        var consequence = voucher.IsActive
            ? "e deixará de poder ser utilizado"
            : "e voltará a poder ser utilizado";

        var confirmed =
            await DialogService.ShowMessageBoxAsync(
                "ATENÇÃO",
                $"O voucher \"{voucher.Title}\" será " +
                $"{action} {consequence}. Deseja continuar?",
                yesText: voucher.IsActive
                    ? "DESATIVAR"
                    : "REATIVAR",
                cancelText: "Cancelar");

        if (confirmed is not true)
            return;

        await OnStatusChangeAsync(voucher);
    }
    private async Task OnStatusChangeAsync(AdminVoucherListItem voucher)
    {
        var wasActive = voucher.IsActive;

        VouchersBeingUpdated.Add(voucher.Id);

        try
        {
            var result = wasActive
                ? await Handler.DeactivateAsync(
                    new DeactivateVoucherRequest
                    {
                        Id = voucher.Id
                    })
                : await Handler.ActivateAsync(
                    new ActivateVoucherRequest
                    {
                        Id = voucher.Id
                    });

            if (!result.IsSuccess)
            {
                Snackbar.Add(
                    result.Message ??
                    "Não foi possível alterar o estado do voucher",
                    Severity.Error);

                return;
            }

            voucher.IsActive = !wasActive;

            Snackbar.Add(
                result.Message ??
                (wasActive
                    ? $"Voucher {voucher.Title} desativado"
                    : $"Voucher {voucher.Title} reativado"),
                Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            VouchersBeingUpdated.Remove(voucher.Id);
        }
    }
}