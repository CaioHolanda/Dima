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

var enableSwagger =
    app.Environment.IsDevelopment() ||
    app.Configuration.GetValue<bool>("EnableSwagger");

if (enableSwagger)
    app.ConfigureDevEnvironment();

app.UseCors(ApiConfiguration.CorsPolicyName);
app.UserSecurity();
app.MapEndpoints();

app.Run();


