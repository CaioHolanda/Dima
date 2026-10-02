using Dima.Api;
using Dima.Api.Common.Api;
using Dima.Api.Data.Seed;
using Dima.Api.Endpoints;
using Dima.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
        options.IncludeScopes = false;
    });
}
else
{
    builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
}
builder.AddConfiguration();
builder.AddSecurity();
builder.AddDataContexts();
builder.AddCrossOrigin();
builder.AddDocumentation();
builder.AddServices();
builder.AddEmailServices();

var app = builder.Build();

if (args.Contains("--schedule-existing-orders", StringComparer.Ordinal))
{
    await using var scope = app.Services.CreateAsyncScope();
    var count = await scope.ServiceProvider.GetRequiredService<Dima.Api.Services.OrderExpirationBackfill>()
        .RunAsync();
    app.Logger.LogInformation("Agendados {Count} pedidos pendentes. Nenhum serviço HTTP foi iniciado.", count);
    return;
}

app.UseMiddleware<RequestObservabilityMiddleware>();
app.ConfigureDevEnvironment();

app.UseCors(ApiConfiguration.CorsPolicyName);
app.UserSecurity();
await app.SeedRolesAsync();
app.MapEndpoints();

app.MapGet("/ping", () => Results.Ok(new
{
    status = "Dima API is running",
    environment = app.Environment.EnvironmentName
})).AllowAnonymous();

app.Run();


