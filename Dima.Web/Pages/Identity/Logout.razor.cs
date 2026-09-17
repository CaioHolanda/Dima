using Dima.Core.Handlers;
using Dima.Web.Security;
using Microsoft.AspNetCore.Components;

namespace Dima.Web.Pages.Identity;

public partial class LogoutPage : ComponentBase
{
    [Inject]
    public IAccountHandler Handler { get; set; } = null!;

    [Inject]
    public ICookieAuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;

    public bool IsBusy { get; private set; } = true;
    public bool HasError { get; private set; }
    private bool _logoutInProgress;

    // Render the pending state before starting the request.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LogoutAsync();
            StateHasChanged();
        }
    }

    public async Task LogoutAsync()
    {
        if (_logoutInProgress) return;
        _logoutInProgress = true;
        IsBusy = true;
        HasError = false;
        try
        {
            await Handler.LogoutAsync();
            AuthenticationStateProvider.ClearAuthenticationState();
        }
        catch
        {
            HasError = true;
        }
        finally
        {
            IsBusy = false;
            _logoutInProgress = false;
        }
    }
}
