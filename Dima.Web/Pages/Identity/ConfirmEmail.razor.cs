using Dima.Core.Handlers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Identity;

public partial class ConfirmEmail : ComponentBase
{
    [Inject]
    public IAccountHandler Handler { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "userId")]
    public string UserId { get; set; } = string.Empty;

    [SupplyParameterFromQuery(Name = "code")]
    public string Code { get; set; } = string.Empty;

    public bool IsBusy { get; set; } = true;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Color ResultColor { get; set; }

    public string ResultIcon { get; set; } = Icons.Material.Filled.CheckCircle;

    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId) ||
            string.IsNullOrWhiteSpace(Code))
        {
            ShowError(
                "[E097] Link inválido",
                "O link de confirmação está incompleto.");

            return;
        }

        try
        {
            var result = await Handler.ConfirmEmailAsync(
                UserId,
                Code);

            if (result.IsSuccess)
            {
                Title = "E-mail confirmado";

                Message =
                    "Sua conta foi ativada com sucesso. Agora você pode realizar o login.";

                ResultColor = Color.Success;

                ResultIcon = Icons.Material.Filled.MarkEmailRead;
            }
            else
            {
                ShowError(
                    "[E098] Não foi possível confirmar o e-mail",
                    result.Message);
            }
        }
        catch
        {
            ShowError(
                "[E099] Erro inesperado",
                "Não foi possível confirmar seu e-mail.");
        }

        IsBusy = false;
    }

    void ShowError(
        string title,
        string message)
    {
        Title = title;

        Message = message;

        ResultColor = Color.Error;

        ResultIcon = Icons.Material.Filled.Error;

        IsBusy = false;
    }

    void NavigateToLogin()
    {
        NavigationManager.NavigateTo("/login");
    }
}