using Dima.Api;
using Dima.Api.Common.Api;
using Dima.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.AddConfiguration();
builder.AddSecurity();
builder.AddDataContexts();
builder.AddCrossOrigin();
builder.AddDocumentation();
builder.AddServices();

var app = builder.Build();

app.ConfigureDevEnvironment();

app.UseCors(ApiConfiguration.CorsPolicyName);
app.UserSecurity();
app.MapEndpoints();

app.MapGet("/ping", () => Results.Ok(new
{
    status = "Dima API is running",
    environment = app.Environment.EnvironmentName
}));

app.Run();


