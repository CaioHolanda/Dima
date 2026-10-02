using System.Net;
using System.Text;
using System.Text.Json;
using Dima.Core.Enums;
using Dima.Core.Requests.Order;
using Dima.Core.Requests.Products;
using Dima.Core.Requests.Payment;
using Dima.Web.Handlers;
using Dima.Web.Security;
using ProductHandler = Dima.Web.Handlers.ProductHandler;

namespace Dima.Tests.TechnicalCleanup;

public class HttpContractTests
{
    [Theory]
    [InlineData(404, "{\"data\":null,\"code\":404,\"message\":\"Produto inexistente\"}", "Produto inexistente")]
    [InlineData(403, "", "[E066]")]
    [InlineData(500, "<html>internal error</html>", "[E066]")]
    [InlineData(400, "{\"title\":\"Bad request\",\"status\":400}", "[E066]")]
    [InlineData(200, "{\"data\":null,\"code\":400,\"message\":\"Erro\"}", "[E066]")]
    [InlineData(200, "{}", "[E066]")]
    [InlineData(200, "null", "[E066]")]
    [InlineData(200, "invalid", "[E066]")]
    public async Task ProductLookupPreservesHttpErrorsAndRejectsInvalidSuccess(int status, string body, string message)
    {
        using var transport = new Transport(status, body);
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.example/") };
        var result = await new ProductHandler(new Factory(client)).GetBySlugAsync(new GetProductBySlugRequest { Slug = "plano" });
        Assert.Equal(status == 200 ? 502 : status, result.Code);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
        Assert.Contains(message, result.Message);
        Assert.True(transport.Content!.Disposed);
    }

    [Fact]
    public async Task ProductListingSendsPaginationAndPreservesMetadata()
    {
        using var transport = new Transport(200, "{\"data\":[],\"code\":200,\"totalCount\":42,\"currentPage\":3,\"pageSize\":10}");
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.example/") };
        var result = await new ProductHandler(new Factory(client)).GetAllAsync(new GetAllProductsRequest { PageNumber = 3, PageSize = 10 });
        Assert.Equal("?pageNumber=3&pageSize=10", transport.Uri!.Query);
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.TotalCount);
        Assert.Equal(3, result.CurrentPage);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(5, result.TotalPages);
    }

    [Fact]
    public async Task CheckoutPreservesDomainErrorAndDoesNotExposeRawBody()
    {
        using var transport = new Transport(409, "{\"data\":null,\"code\":409,\"message\":\"Pedido expirado\"}");
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.example/") };
        var handler = new StripePaymentHandler(new Factory(client));
        var result = await handler.CreateSessionAsync(new CreatePaymentSessionRequest { OrderNumber = "ORDER020" });
        Assert.Equal(409, result.Code);
        Assert.Equal("Pedido expirado", result.Message);
        transport.Body = "<html>secret stack trace</html>";
        transport.Status = 500;
        result = await handler.CreateSessionAsync(new CreatePaymentSessionRequest { OrderNumber = "ORDER020" });
        Assert.Equal(500, result.Code);
        Assert.DoesNotContain("secret", result.Message);
    }

    [Fact]
    public async Task TransportFailureCannotBeReportedAsSuccessByBody()
    {
        using var transport = new Transport(500, "{\"data\":{\"slug\":\"plano\"},\"code\":200,\"message\":\"\"}");
        using var client = new HttpClient(transport) { BaseAddress = new Uri("https://api.example/") };
        var result = await new ProductHandler(new Factory(client)).GetBySlugAsync(new GetProductBySlugRequest { Slug = "plano" });
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.Code);
        Assert.Null(result.Data);
        Assert.Contains("[E066]", result.Message);
    }

    [Fact]
    public async Task CookieHandlerSendsCorrectAjaxHeaderAndPreservesUnauthorizedBehavior()
    {
        using var transport = new Transport(200, "{}");
        var signals = new SessionSignals();
        var rejected = 0;
        signals.Unauthorized += () => rejected++;
        using var cookies = new CookieHandler(signals) { InnerHandler = transport };
        using var client = new HttpClient(cookies) { BaseAddress = new Uri("https://api.example/") };
        using var ok = await client.GetAsync("v1/products");
        Assert.Equal("XMLHttpRequest", transport.AjaxHeader);
        Assert.False(transport.MisspelledHeader);
        transport.Status = 401;
        using var login = await client.PostAsync("v1/identity/login-user", null);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        Assert.Equal(0, rejected);
        var error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("v1/orders"));
        Assert.Equal(HttpStatusCode.Unauthorized, error.StatusCode);
        Assert.Equal(1, rejected);
    }

    [Fact]
    public void OrderStatusStillUsesOriginalNumericContract()
    {
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, Enum.GetValues<EOrderStatus>().Select(x => (int)x));
        Assert.Equal("1", JsonSerializer.Serialize(EOrderStatus.WaitingPayment));
        Assert.Equal(EOrderStatus.WaitingPayment, JsonSerializer.Deserialize<EOrderStatus>("1"));
    }

    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class TrackedContent(string body) : StringContent(body, Encoding.UTF8, "application/json")
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }

    private sealed class Transport(int status, string body) : HttpMessageHandler
    {
        public int Status { get; set; } = status;
        public string Body { get; set; } = body;
        public Uri? Uri { get; private set; }
        public TrackedContent? Content { get; private set; }
        public string? AjaxHeader { get; private set; }
        public bool MisspelledHeader { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            AjaxHeader = request.Headers.TryGetValues("X-Requested-With", out var values) ? values.Single() : null;
            MisspelledHeader = request.Headers.Contains("X-Requested-Witdh");
            Content = new TrackedContent(Body);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)Status) { Content = Content });
        }
    }
}
