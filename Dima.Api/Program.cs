using Dima.Api;
using Dima.Api.Common.Api;
using Dima.Api.Data.Seed;
using Dima.Api.Endpoints;
using Dima.Api.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.AddConfiguration();
builder.AddSecurity();
builder.AddDataContexts();
builder.AddCrossOrigin();
builder.AddDocumentation();
builder.AddServices();
builder.AddEmailServices();

var app = builder.Build();

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


