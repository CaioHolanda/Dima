using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Orders;

public partial class ListOrdersPage : ComponentBase, IAsyncDisposable
{
    public bool IsBusy { get; set; }

    public List<Order> Orders { get; set; } = [];

    public string? VerificationWarning { get; set; }

    private readonly CancellationTokenSource _refreshCancellation = new();
    private Task? _refreshTask;
    private bool _disposed;

    [Inject]
    public IOrderHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrdersAsync(initialLoad: true);

        if (!_disposed)
            _refreshTask = RefreshPeriodicallyAsync(
                _refreshCancellation.Token);
    }

    private async Task LoadOrdersAsync(bool initialLoad)
    {
        if (initialLoad)
            IsBusy = true;

        try
        {
            var result = await Handler.GetAllAsync(
                new GetAllOrdersRequest
                {
                    PageNumber = 1,
                    PageSize = 100
                });

            if (_disposed)
                return;

            if (result.IsSuccess)
            {
                Orders = result.Data ?? [];

                VerificationWarning =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? null
                        : result.Message;

                return;
            }

            VerificationWarning =
                "Não foi possível atualizar os pedidos. " +
                "Uma nova tentativa será feita automaticamente.";

            if (initialLoad)
            {
                Snackbar.Add(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Não foi possível carregar os pedidos."
                        : result.Message,
                    Severity.Error);
            }
        }
        catch (Exception)
        {
            if (_disposed)
                return;

            VerificationWarning =
                "Não foi possível atualizar os pedidos. " +
                "Verifique sua conexão. Tentaremos novamente automaticamente.";

            if (initialLoad)
            {
                Snackbar.Add(
                    "Não foi possível carregar os pedidos.",
                    Severity.Error);
            }
        }
        finally
        {
            if (!_disposed)
                IsBusy = false;
        }
    }

    private async Task RefreshPeriodicallyAsync(
        CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(30));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await InvokeAsync(async () =>
                {
                    if (_disposed)
                        return;

                    var needsRefresh =
                        Orders.Any(order =>
                            order.Status == EOrderStatus.WaintingPayment)
                        || !string.IsNullOrWhiteSpace(VerificationWarning);

                    if (!needsRefresh)
                        return;

                    await LoadOrdersAsync(initialLoad: false);

                    if (!_disposed)
                        StateHasChanged();
                });
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Encerramento normal ao sair da página.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        _refreshCancellation.Cancel();

        try
        {
            if (_refreshTask is not null)
                await _refreshTask;
        }
        finally
        {
            _refreshCancellation.Dispose();
        }
    }
}