using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Dima.Core.Requests.Order;
using Microsoft.AspNetCore.Components;

namespace Dima.Web.Pages.Admin.Orders;

public class AdminOrderDetailsPage : ComponentBase
{
    [Parameter] public long Id { get; set; }
    [SupplyParameterFromQuery(Name = "pageNumber")] public int? Page { get; set; }
    [SupplyParameterFromQuery(Name = "pageSize")] public int? Size { get; set; }
    [SupplyParameterFromQuery(Name = "searchTerm")] public string? Search { get; set; }
    [SupplyParameterFromQuery(Name = "status")] public int? Status { get; set; }
    [Inject] public IAdminOrderHandler Handler { get; set; } = null!;
    public AdminOrderDetails? Order { get; private set; }
    public bool IsBusy { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    private int _version;
    public string BackUrl => "/admin/orders" + Dima.Web.Handlers.AdminQuery.Build(new GetAllAdminOrdersRequest
    {
        PageNumber = Page ?? 1, PageSize = Size ?? 25, SearchTerm = Search,
        Status = Status.HasValue && Enum.IsDefined(typeof(EOrderStatus), Status.Value) ? (EOrderStatus)Status.Value : null
    });
    protected override Task OnParametersSetAsync() => LoadAsync();
    public async Task LoadAsync()
    {
        var version = ++_version;
        IsBusy = true;
        Order = null;
        try
        {
            var result = await Handler.GetByIdAsync(Id);
            if (version != _version) return;
            Order = result.IsSuccess ? result.Data : null;
            ErrorMessage = string.IsNullOrWhiteSpace(result.Message) ? "Pedido não encontrado." : result.Message;
        }
        catch
        {
            if (version == _version) ErrorMessage = "Não foi possível consultar o pedido. Tente novamente.";
        }
        finally { if (version == _version) IsBusy = false; }
    }
    public static string Text(string? value) => string.IsNullOrWhiteSpace(value) ? "Não registrado" : value;
    public static string Date(DateTime? value) => value?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Não registrado";
    public static string UtcDate(DateTimeOffset? value) => value?.UtcDateTime.ToString("dd/MM/yyyy HH:mm:ss") ?? "Não registrado";
    public string GatewayText => Order?.Gateway switch
    {
        null => "Não definido",
        EPaymentGateway.NotApplicable => "Não aplicável — pedido gratuito",
        var gateway => gateway.ToString()!
    };
    public string VoucherText => !string.IsNullOrWhiteSpace(Order?.VoucherCode)
        ? Order.VoucherCode : Order?.DiscountAmount > 0 ? "Código histórico não registrado" : "Não registrado";
    public string VoucherRule => Order?.VoucherDiscountTypeSnapshot switch
    {
        EVoucherDiscountType.Percentage => $"{Order.VoucherValueSnapshot:0.##}%",
        EVoucherDiscountType.FixedAmount => ListAdminOrdersPage.FormatCurrency(Order.VoucherValueSnapshot ?? 0),
        _ => "Tipo de desconto não registrado"
    };
    public string ReconciliationText => Order?.Gateway == EPaymentGateway.NotApplicable
        ? "Pedido gratuito: pagamento externo não se aplica."
        : !string.IsNullOrWhiteSpace(Order?.ExternalReference)
            ? "Há uma referência de pagamento registrada para conferência no provedor."
            : Order?.Status == EOrderStatus.WaintingPayment
                ? "Pedido aguardando pagamento, sem referência confirmada. A ausência de referência não comprova falha no provedor."
                : "Não há referência de pagamento registrada. Se necessário, confira o pedido no provedor antes de concluir sua situação financeira.";
}