using Dima.Core.Handlers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Identity;

public partial class RegistrationCompleted : ComponentBase
{
    [Inject]
    public IAccountHandler Handler { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "email")]
    public string Email { get; set; } = string.Empty;

    public bool IsBusy { get; set; }

    public string StatusMessage { get; set; } = string.Empty;

    public Severity StatusSeverity { get; set; } = Severity.Info;

    public void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }

    public async Task ResendConfirmationEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusSeverity = Severity.Error;
            StatusMessage =
                "[E101] Não foi possível identificar o e-mail cadastrado.";

            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;

        try
        {
            var result =
                await Handler.ResendConfirmationEmailAsync(Email);

            StatusSeverity = result.IsSuccess
                ? Severity.Success
                : Severity.Error;

            StatusMessage = result.Message;
        }
        catch
        {
            StatusSeverity = Severity.Error;
            StatusMessage =
                "[E100] Não foi possível reenviar o e-mail de confirmação.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}