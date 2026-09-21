using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Services;
using Dima.Core.Handlers;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Stripe;

var builder = FunctionsApplication.CreateBuilder(args);
// Azure Functions loads local.settings.json for local use; Azure uses app settings.
// No API startup, role seeding, SQL migration, or hosted SQL polling occurs here.
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.Configure<ApiOptions>(builder.Configuration);
builder.Services.Configure<OrderExpirationOptions>(builder.Configuration.GetSection(OrderExpirationOptions.SectionName));
builder.Services.Configure<OrderExpirationQueueOptions>(options =>
{
    options.ConnectionString = builder.Configuration["OrderExpirationStorage"] ?? string.Empty;
    options.QueueName = builder.Configuration["OrderExpirationQueueName"] ?? "dima-order-expiration";
});
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<IStripeClient>(services =>
    new StripeClient(services.GetRequiredService<IOptions<ApiOptions>>().Value.StripeApiKey));
builder.Services.AddTransient<IPaymentHandler, StripePaymentHandler>();
builder.Services.AddTransient<OrderExpirationService>();
builder.Services.AddSingleton<IOrderExpirationScheduler, QueueOrderExpirationScheduler>();
builder.Services.AddTransient<ScheduledOrderExpirationProcessor>();
builder.Build().Run();