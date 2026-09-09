using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Orders;

public partial class DetailsPage : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public string Number { get; set; } = string.Empty;

    public Order Order { get; set; } = null!;

    public string? VerificationWarning { get; set; }

    private readonly CancellationTokenSource _refreshCancellation = new();
    private Task? _refreshTask;
    private bool _disposed;
    private long _stateVersion;

    [Inject]
    public IOrderHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await LoadOrderAsync(initialLoad: true);

        if (!_disposed)
        {
            _refreshTask = RefreshPeriodicallyAsync(
                _refreshCancellation.Token);
        }
    }

    private async Task LoadOrderAsync(bool initialLoad)
    {
        var version = _stateVersion;

        try
        {
            var result = await Handler.GetByNumberAsync(
                new GetOrderByNumberRequest
                {
                    Number = Number
                });

            if (_disposed || version != _stateVersion)
                return;

            if (result.IsSuccess && result.Data is not null)
            {
                Order = result.Data;

                VerificationWarning =
                    string.IsNullOrWhiteSpace(result.Message)
                        ? null
                        : result.Message;

                return;
            }

            VerificationWarning =
                "Não foi possível atualizar este pedido. " +
                "Uma nova tentativa será feita automaticamente.";

            if (initialLoad)
            {
                Snackbar.Add(
                    string.IsNullOrWhiteSpace(result.Message)
                        ? "Não foi possível carregar o pedido."
                        : result.Message,
                    Severity.Error);
            }
        }
        catch (Exception)
        {
            if (_disposed || version != _stateVersion)
                return;

            VerificationWarning =
                "Não foi possível atualizar este pedido. " +
                "Verifique sua conexão. Tentaremos novamente automaticamente.";

            if (initialLoad)
            {
                Snackbar.Add(
                    "Não foi possível carregar o pedido.",
                    Severity.Error);
            }
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
                        Order?.Status == EOrderStatus.WaintingPayment
                        || !string.IsNullOrWhiteSpace(VerificationWarning);

                    if (!needsRefresh)
                        return;

                    await LoadOrderAsync(initialLoad: false);

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

    public void RefreshState(Order order)
    {
        if (_disposed)
            return;

        // Invalida o resultado de uma consulta anterior
        // à atualização feita por uma ação da página.
        _stateVersion++;

        Order = order;
        VerificationWarning = null;

        StateHasChanged();
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