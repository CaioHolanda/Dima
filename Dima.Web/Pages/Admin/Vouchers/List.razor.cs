using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Vouchers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Vouchers;

public partial class ListAdminVouchersPage
    : ComponentBase
{
    public bool IsBusy { get; set; }
    public List<Voucher> Vouchers { get; set; } = [];
    public string SearchTerm { get; set; } = string.Empty;

    [Inject]
    public IAdminVoucherHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    public Func<Voucher, bool> Filter => voucher =>
    {
        if (string.IsNullOrWhiteSpace(SearchTerm))
            return true;

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
               || (voucher.AssignedUserId?.Contains(
                       SearchTerm,
                       StringComparison.OrdinalIgnoreCase)
                   ?? false);
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
}