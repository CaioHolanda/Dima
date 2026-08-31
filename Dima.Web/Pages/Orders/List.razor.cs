using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Dima.Web.Pages.Orders;

public partial class ListOrdersPage : ComponentBase
{
    public bool IsBusy { get; set; }

    public List<Order> Orders { get; set; } = [];

    [Inject]
    public IOrderHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        IsBusy = true;

        try
        {
            var result = await Handler.GetAllAsync(
                new GetAllOrdersRequest
                {
                    PageNumber = 1,
                    PageSize = 100
                });

            if (result.IsSuccess)
            {
                Orders = result.Data ?? [];
                return;
            }

            Snackbar.Add(
                result.Message ??
                "Não foi possível carregar os pedidos.",
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