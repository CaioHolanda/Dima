using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;
using Dima.Core.Requests.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Vouchers;

public partial class ListAdminVouchersPage
    : Dima.Web.Pages.Admin.AdminListPage<AdminVoucherListItem>
{
    public List<AdminVoucherListItem> Vouchers { get; set; } = [];
    public HashSet<long> VouchersBeingUpdated { get; set; } = [];

    [Inject]
    public IAdminVoucherHandler Handler { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task<Dima.Core.Responses.PagedResponse<List<AdminVoucherListItem>?>> FetchAsync()
    {
        var request = new GetAllAdminVouchersRequest();
        Configure(request);
        return await Handler.GetAllForAdminAsync(request);
    }
    protected override void SetItems(List<AdminVoucherListItem> items) => Vouchers = items;

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
            await ReloadAsync();

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