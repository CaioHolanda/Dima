using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Dima.Web;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Dima.Web.Security;
using Dima.Core.Handlers;
using Dima.Web.Handlers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
var configuredBackendUrl =
    builder.Configuration.GetValue<string>("BackendUrl");

if (!string.IsNullOrWhiteSpace(configuredBackendUrl))
    Configuration.BackendUrl = configuredBackendUrl; Configuration.StripePublickey=builder.Configuration.GetValue<string>("StripePublicKey")??string.Empty;

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped<CookieHandler>();

builder.Services.AddMudServices();

var configuredAddress =
    builder.HostEnvironment.IsDevelopment()
        ? Configuration.BackendUrl
        : builder.HostEnvironment.BaseAddress;

var backendUrl =
    NormalizeApiBaseAddress(configuredAddress);

Console.WriteLine($"API BASE ADDRESS: {backendUrl}");

builder.Services
    .AddHttpClient(Configuration.HttpClientName, opt =>
    {
        opt.BaseAddress = backendUrl;
    })
    .AddHttpMessageHandler<CookieHandler>();

builder.Services.AddTransient<IAccountHandler,      AccountHandler      >();
builder.Services.AddTransient<ITransactionHandler,  TransactionHandler  >();
builder.Services.AddTransient<IOrderHandler,        OrderHandler        >();
builder.Services.AddTransient<IPaymentHandler,      StripePaymentHandler>(); 
builder.Services.AddTransient<IProductHandler,      ProductHandler      >();
builder.Services.AddTransient<IVoucherHandler,      VoucherHandler      >();
builder.Services.AddTransient<ICategoryHandler,     CategoryHandler     >();
builder.Services.AddTransient<IReportHandler,       ReportHandler       >();
builder.Services.AddTransient<IAdminProductHandler, AdminProductHandler >();
builder.Services.AddTransient<IAdminVoucherHandler, AdminVoucherHandler >();
builder.Services.AddTransient<IAdminUserHandler,    AdminUserHandler    >();
builder.Services.AddTransient<IAdminOrderHandler,   AdminOrderHandler   >();


builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();
builder.Services.AddScoped(
    x => (ICookieAuthenticationStateProvider)x
         .GetRequiredService<AuthenticationStateProvider>());

builder.Services.AddLocalization();
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("pt-BR");
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("pt-BR");


await builder.Build().RunAsync();
static Uri NormalizeApiBaseAddress(string address)
{
    if (!Uri.TryCreate(
            address.Trim(),
            UriKind.Absolute,
            out var uri))
    {
        throw new InvalidOperationException(
            $"BackendUrl inválido: '{address}'");
    }

    var path = uri.AbsolutePath.TrimEnd('/');

    while (path.EndsWith(
        "/api",
        StringComparison.OrdinalIgnoreCase))
    {
        path = path[..^4].TrimEnd('/');
    }

    var normalizedUri = new UriBuilder(uri)
    {
        Path = $"{path}/api/",
        Query = string.Empty,
        Fragment = string.Empty
    };

    return normalizedUri.Uri;
}