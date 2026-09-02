using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Dima.Web.Pages.Admin.Orders;

public partial class ListAdminOrdersPage : ComponentBase
{
    public bool IsBusy { get; set; }

    public List<AdminOrderListItem> Orders { get; set; } = [];

    public string SearchTerm { get; set; } = string.Empty;

    [Inject]
    public IAdminOrderHandler Handler { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    public Func<AdminOrderListItem, bool> Filter =>
        order =>
        {
            if (string.IsNullOrWhiteSpace(SearchTerm))
                return true;

            var status = GetStatusText(order.Status);

            return order.Number.Contains(
                       SearchTerm,
                       StringComparison.OrdinalIgnoreCase)
                   || order.UserEmail.Contains(
                       SearchTerm,
                       StringComparison.OrdinalIgnoreCase)
                   || order.ProductName.Contains(
                       SearchTerm,
                       StringComparison.OrdinalIgnoreCase)
                   || status.Contains(
                       SearchTerm,
                       StringComparison.OrdinalIgnoreCase);
        };

    protected override async Task OnInitializedAsync()
    {
        IsBusy = true;

        try
        {
            var result = await Handler.GetAllAsync(
                new GetAllAdminOrdersRequest
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
                "[E192] Não foi possível carregar os pedidos",
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

    public static string FormatDateTime(DateTime date)
    {
        return date.ToString("dd/MM/yyyy HH:mm");
    }

    public static string FormatDate(DateTime? date)
    {
        return date?.ToString("dd/MM/yyyy") ?? "-";
    }
    public static string FormatAccessStart(
    AdminOrderListItem order)
    {
        if (order.AccessStartsAt is not null)
        {
            return order.AccessStartsAt.Value
                .ToString("dd/MM/yyyy");
        }

        return order.Status == EOrderStatus.WaintingPayment
            ? "Aguardando pagamento"
            : "-";
    }
    public static string FormatAccessEnd(
        AdminOrderListItem order)
    {
        if (order.AccessStartsAt is null)
        {
            return order.Status == EOrderStatus.WaintingPayment
                ? "Aguardando pagamento"
                : "-";
        }
            return order.AccessEndsAt?.ToString("dd/MM/yyyy")
                   ?? "-";
    }

    public static string FormatCurrency(decimal value)
    {
        return value.ToString(
            "C2",
            CultureInfo.GetCultureInfo("pt-BR"));
    }

    public static string GetStatusText(EOrderStatus status)
    {
        return status switch
        {
            EOrderStatus.WaintingPayment =>
                "Aguardando pagamento",

            EOrderStatus.Paid =>
                "Pago",

            EOrderStatus.Canceled =>
                "Cancelado",

            EOrderStatus.Refunded =>
                "Reembolsado",

            EOrderStatus.RefundPending =>
                "Reembolso em processamento",

            _ => "Desconhecido"
        };
    }

    public static Color GetStatusColor(EOrderStatus status)
    {
        return status switch
        {
            EOrderStatus.WaintingPayment =>
                Color.Warning,

            EOrderStatus.Paid =>
                Color.Success,

            EOrderStatus.Canceled =>
                Color.Error,

            EOrderStatus.Refunded =>
                Color.Info,

            EOrderStatus.RefundPending =>
                Color.Warning,

            _ => Color.Default
        };
    }
    public static string GetRefundReasonText(
        ERefundReason reason)
    {
        return reason switch
        {
            ERefundReason.NotUsingProduct =>
                "Não está utilizando",

            ERefundReason.NotAsExpected =>
                "Não atendeu às expectativas",

            ERefundReason.PurchasedByMistake =>
                "Compra por engano",

            ERefundReason.TechnicalIssue =>
                "Problema técnico",

            ERefundReason.Price =>
                "Preço",

            ERefundReason.Other =>
                "Outro",

            _ => "-"
        };
    }
}