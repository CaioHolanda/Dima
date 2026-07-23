using Dima.Core.Handlers;
using Dima.Core.Requests.Account;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Identity;

public partial class ResetPasswordPage : ComponentBase
{
    #region Dependencies

    [Inject]
    public IAccountHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    #endregion

    #region Query Parameters

    [Parameter]
    [SupplyParameterFromQuery(Name = "email")]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "code")]
    public string? Code { get; set; }

    #endregion

    #region Properties

    public ResetPasswordRequest InputModel { get; set; } = new();

    public bool IsBusy { get; set; }

    public bool PasswordChanged { get; set; }

    public bool HasValidParameters =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Code);

    public bool IsNewPasswordVisible { get; set; }

    public bool IsConfirmPasswordVisible { get; set; }

    public InputType NewPasswordInputType =>
        IsNewPasswordVisible
            ? InputType.Text
            : InputType.Password;

    public InputType ConfirmPasswordInputType =>
        IsConfirmPasswordVisible
            ? InputType.Text
            : InputType.Password;

    public string NewPasswordVisibilityIcon =>
        IsNewPasswordVisible
            ? Icons.Material.Filled.VisibilityOff
            : Icons.Material.Filled.Visibility;

    public string ConfirmPasswordVisibilityIcon =>
        IsConfirmPasswordVisible
            ? Icons.Material.Filled.VisibilityOff
            : Icons.Material.Filled.Visibility;

    public string NewPasswordVisibilityAriaLabel =>
        IsNewPasswordVisible
            ? "Ocultar nova senha"
            : "Exibir nova senha";

    public string ConfirmPasswordVisibilityAriaLabel =>
        IsConfirmPasswordVisible
            ? "Ocultar confirmação da senha"
            : "Exibir confirmação da senha";

    #endregion

    #region Overrides

    protected override void OnParametersSet()
    {
        if (!HasValidParameters)
            return;

        InputModel.Email = Email!;
        InputModel.ResetCode = Code!;
    }

    #endregion

    #region Methods

    public void ToggleNewPasswordVisibility()
    {
        IsNewPasswordVisible = !IsNewPasswordVisible;
    }

    public void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordVisible = !IsConfirmPasswordVisible;
    }

    public async Task OnValidSubmitAsync()
    {
        if (!HasValidParameters)
        {
            Snackbar.Add(
                "O link de redefinição é inválido.",
                Severity.Error);

            return;
        }

        IsBusy = true;

        try
        {
            var result =
                await Handler.ResetPasswordAsync(InputModel);

            if (result.IsSuccess)
            {
                PasswordChanged = true;

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