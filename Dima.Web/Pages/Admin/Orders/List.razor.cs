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
    [SupplyParameterFromQuery(Name = "pageNumber")] public int? InitialPage { get; set; }
    [SupplyParameterFromQuery(Name = "pageSize")] public int? InitialSize { get; set; }
    [SupplyParameterFromQuery(Name = "searchTerm")] public string? InitialSearch { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public int? InitialStatus { get; set; }

    protected override Task OnInitializedAsync() => Task.CompletedTask;
    protected override async Task OnParametersSetAsync()
    {
        RestoreOrderList(InitialPage ?? 1, InitialSize ?? 25, InitialSearch,
            InitialStatus.HasValue && Enum.IsDefined(typeof(EOrderStatus), InitialStatus.Value)
                ? (EOrderStatus)InitialStatus.Value : null);
        await ReloadAsync();
    }
    public string DetailsUrl(long id)
    {
        var request = new GetAllAdminOrdersRequest();
        Configure(request);
        return $"/admin/orders/{id}" + Dima.Web.Handlers.AdminQuery.Build(request);
    }

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
        return Dima.Core.Common.Time.UtcInstant.FormatLocal(date, "dd/MM/yyyy HH:mm");
    }

    public static string FormatDate(DateTime? date)
    {
        return Dima.Core.Common.Time.UtcInstant.FormatLocal(date, "dd/MM/yyyy");
    }
    public static string FormatAccessStart(
    AdminOrderListItem order)
    {
        if (order.AccessStartsAt is not null)
        {
            return Dima.Core.Common.Time.UtcInstant.FormatLocal(order.AccessStartsAt, "dd/MM/yyyy");
        }

        return order.Status == EOrderStatus.WaitingPayment
            ? "Aguardando pagamento"
            : "-";
    }
    public static string FormatAccessEnd(
        AdminOrderListItem order)
    {
        if (order.AccessStartsAt is null)
        {
            return order.Status == EOrderStatus.WaitingPayment
                ? "Aguardando pagamento"
                : "-";
        }
            return Dima.Core.Common.Time.UtcInstant.FormatLocal(order.AccessEndsAt, "dd/MM/yyyy");
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
            EOrderStatus.WaitingPayment =>
                "Aguardando pagamento",

            EOrderStatus.Paid =>
                "Pago",

            EOrderStatus.Canceled =>
                "Cancelado",

            EOrderStatus.Refunded =>
                "Reembolsado",

            EOrderStatus.RefundPending =>
                "Reembolso em processamento",

            EOrderStatus.Expired => "Expirado",
            _ => "Desconhecido"
        };
    }

    public static Color GetStatusColor(EOrderStatus status)
    {
        return status switch
        {
            EOrderStatus.WaitingPayment =>
                Color.Warning,

            EOrderStatus.Paid =>
                Color.Success,

            EOrderStatus.Canceled =>
                Color.Error,

            EOrderStatus.Refunded =>
                Color.Info,

            EOrderStatus.RefundPending =>
                Color.Warning,

            EOrderStatus.Expired => Color.Default,
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