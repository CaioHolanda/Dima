using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Globalization;

namespace Dima.Web.Pages.Admin.Orders;

public partial class ListAdminOrdersPage : Dima.Web.Pages.Admin.AdminListPage<AdminOrderListItem>
{
    public List<AdminOrderListItem> Orders { get; set; } = [];

    [Inject]
    public IAdminOrderHandler Handler { get; set; } = null!;

    protected override async Task<Dima.Core.Responses.PagedResponse<List<AdminOrderListItem>?>> FetchAsync()
    {
        var request = new GetAllAdminOrdersRequest();
        Configure(request);
        return await Handler.GetAllAsync(request);
    }
    protected override void SetItems(List<AdminOrderListItem> items) => Orders = items;

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