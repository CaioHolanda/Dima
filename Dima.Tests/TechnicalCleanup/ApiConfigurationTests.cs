using Dima.Api.Common.Api;
using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Core.Requests.Payment;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Stripe;

namespace Dima.Tests.TechnicalCleanup;

public class ApiConfigurationTests
{
    [Fact]
    public void HostsKeepIndependentOptionsAndStripeClientsUsingExistingKeys()
    {
        using var first = CreateProvider("sk_test_first", "https://first.example");
        using var second = CreateProvider("sk_test_second", "https://second.example");
        Assert.Equal("https://first.example", first.GetRequiredService<IOptions<ApiOptions>>().Value.FrontendUrl);
        Assert.Equal("https://second.example", second.GetRequiredService<IOptions<ApiOptions>>().Value.FrontendUrl);
        Assert.Equal("sk_test_first", first.GetRequiredService<IStripeClient>().ApiKey);
        Assert.Equal("sk_test_second", second.GetRequiredService<IStripeClient>().ApiKey);
        Assert.NotSame(first.GetRequiredService<IStripeClient>(), second.GetRequiredService<IStripeClient>());
        Assert.Same(first.GetRequiredService<IStripeClient>(), first.GetRequiredService<IStripeClient>());
    }

    [Fact]
    public async Task MissingStripeKeyStillReturnsConfigurationErrorBeforeCallingStripe()
    {
        using var provider = CreateProvider("", "https://frontend.example");
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var handler = new StripePaymentHandler(db, TimeProvider.System,
            Options.Create(new OrderExpirationOptions()), provider.GetRequiredService<IOptions<ApiOptions>>(),
            provider.GetRequiredService<IStripeClient>());
        var result = await handler.CreateSessionAsync(new CreatePaymentSessionRequest { OrderNumber = "ORDER020" });
        Assert.Equal(500, result.Code);
        Assert.Contains("[E089]", result.Message);
    }

    private static ServiceProvider CreateProvider(string key, string frontend)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StripeApiKey"] = key, ["FrontendUrl"] = frontend,
            ["StripeWebhookSecret"] = "whsec_dt20_test"
        });
        builder.AddConfiguration();
        builder.AddServices();
        return builder.Services.BuildServiceProvider();
    }
}
