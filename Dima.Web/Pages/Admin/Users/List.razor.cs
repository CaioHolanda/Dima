using Dima.Core.Handlers;
using Dima.Core.Models.Account;
using Dima.Core.Requests.Account;
using Dima.Core.Requests.Users;
using Dima.Core.Responses;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Admin.Users;

public partial class ListAdminUsersPage
    : Dima.Web.Pages.Admin.AdminListPage<AdminUserListItem>
{
    public List<AdminUserListItem> Users { get; set; } = [];
    protected HashSet<long> UsersBeingUpdated { get; set; } = [];
    protected HashSet<long> UsersBeingReset { get; set; } = [];

    [Inject]
    public IAdminUserHandler Handler { get; set; } = null!;

    [Inject]
    public IAccountHandler AccountHandler { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task<Dima.Core.Responses.PagedResponse<List<AdminUserListItem>?>> FetchAsync()
    {
        var request = new GetAllAdminUsersRequest();
        Configure(request);
        return await Handler.GetAllAsync(request);
    }
    protected override void SetItems(List<AdminUserListItem> items) => Users = items;

    public static string FormatDate(DateTime? date)
    {
        return Dima.Core.Common.Time.UtcInstant.FormatLocal(date, "dd/MM/yyyy");
    }

    public static string GetAccessUntil(
        AdminUserListItem user)
    {
        // Existe um próximo produto já pago/agendado.
        if (user.NextAccessStartsAt is not null)
        {
            return Dima.Core.Common.Time.UtcInstant.FormatLocal(user.NextAccessEndsAt, "dd/MM/yyyy");
        }

        // Nunca houve acesso pago.
        if (user.AccessStartsAt is null)
            return "-";

        return Dima.Core.Common.Time.UtcInstant.FormatLocal(user.AccessEndsAt, "dd/MM/yyyy");
    }
    protected async Task ToggleUserStatusAsync(AdminUserListItem user)
    {
        if (UsersBeingUpdated.Contains(user.Id))
            return;

        var action = user.IsActive
            ? "desativar"
            : "ativar";

        var confirmed = await DialogService.ShowMessageBoxAsync(
            user.IsActive
                ? "Desativar usuário"
                : "Ativar usuário",
            $"Deseja realmente {action} o usuário {user.Email}?",
            yesText: "Sim",
            cancelText: "Cancelar");

        if (confirmed != true)
            return;

        UsersBeingUpdated.Add(user.Id);

        try
        {
            Response<AdminUserListItem?> response;

            if (user.IsActive)
            {
                response = await Handler.DeactivateAsync(
                    new DeactivateUserRequest
                    {
                        Id = user.Id
                    });
            }
            else
            {
                response = await Handler.ActivateAsync(
                    new ActivateUserRequest
                    {
                        Id = user.Id
                    });
            }

            if (!response.IsSuccess)
            {
                Snackbar.Add(
                    response.Message ?? "Não foi possível alterar o usuário",
                    Severity.Error);

                return;
            }

            user.IsActive = !user.IsActive;
            await ReloadAsync();

            Snackbar.Add(
                response.Message ??
                (user.IsActive
                    ? "Usuário ativado com sucesso"
                    : "Usuário desativado com sucesso"),
                Severity.Success);
        }
        finally
        {
            UsersBeingUpdated.Remove(user.Id);
        }
    }
    protected async Task SendPasswordResetAsync(
    AdminUserListItem user)
    {
        if (!user.IsActive ||
            UsersBeingReset.Contains(user.Id))
            return;

        var confirmed =
            await DialogService.ShowMessageBoxAsync(
                "Redefinir senha",
                $"Enviar um link de redefinição de senha para {user.Email}?",
                yesText: "Enviar",
                cancelText: "Cancelar");

        if (confirmed != true)
            return;

        UsersBeingReset.Add(user.Id);

        try
        {
            var response =
                await AccountHandler.ForgotPasswordAsync(
                    new ForgotPasswordRequest
                    {
                        Email = user.Email
                    });

            if (!response.IsSuccess)
            {
                Snackbar.Add(
                    response.Message ??
                    "[E189] Não foi possível enviar o link de redefinição",
                    Severity.Error);

                return;
            }

            Snackbar.Add(
                $"Link de redefinição solicitado para {user.Email}",
                Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                ex.Message,
                Severity.Error);
        }
        finally
        {
            UsersBeingReset.Remove(user.Id);
        }
    }
}