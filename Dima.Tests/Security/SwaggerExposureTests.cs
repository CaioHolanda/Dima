using System.Net;
using Dima.Api.Common.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dima.Tests.Security;

public sealed class SwaggerExposureTests
{
    [Theory]
    [InlineData("Production", true, false)]
    [InlineData("Production", false, false)]
    [InlineData("Staging", true, false)]
    [InlineData("Development", false, false)]
    [InlineData("Development", true, true)]
    public async Task Documentation_is_available_only_when_enabled_in_development(
        string environment, bool enabled, bool available)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EnableSwagger"] = enabled.ToString()
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        await using var app = builder.Build();
        app.ConfigureDevEnvironment();
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        using var client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
        foreach (var path in new[] { "/swagger/index.html", "/swagger/v1/swagger.json", "/swagger/swagger-ui-bundle.js" })
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(available ? HttpStatusCode.OK : HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
