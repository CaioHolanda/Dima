using Dima.Core.Handlers;
using Dima.Core.Requests.Account;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Identity;

public partial class ForgotPasswordPage : ComponentBase
{
    #region Dependencies

    [Inject]
    public IAccountHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    #endregion

    #region Properties

    public ForgotPasswordRequest InputModel { get; set; } = new();

    public bool IsBusy { get; set; }

    public bool RequestSent { get; set; }

    #endregion

    #region Methods

    public async Task OnValidSubmitAsync()
    {
        IsBusy = true;

        try
        {
            var result =
                await Handler.ForgotPasswordAsync(InputModel);

            if (result.IsSuccess)
            {
                RequestSent = true;

                Snackbar.Add(
                    result.Message,
                    Severity.Success);

                return;
            }

            Snackbar.Add(
                result.Message,
                Severity.Error);
        }
        catch (Exception exception)
        {
            Snackbar.Add(
                exception.Message,
                Severity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    #endregion
}