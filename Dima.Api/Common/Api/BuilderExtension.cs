using Dima.Api.Configuration;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services;
using Dima.Api.Services.Email;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stripe;
using Stripe.Checkout;

namespace Dima.Api.Common.Api
{
    public static class BuilderExtension
    {
        public static void AddConfiguration(this WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<BusinessTimeOptions>()
                .Bind(builder.Configuration.GetSection(BusinessTimeOptions.SectionName))
                .Validate(options => IsValidTimeZone(options.TimeZoneId),
                    "BusinessTime:TimeZoneId deve identificar um fuso válido.")
                .ValidateOnStart();
            builder.Services.Configure<InitialAdminOptions>
                (builder.Configuration.GetSection(InitialAdminOptions.SectionName));
            builder.Services.Configure<ApiOptions>(builder.Configuration);

            builder.Services
                .AddOptions<OrderExpirationOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        OrderExpirationOptions.SectionName))
                .Validate(
                    options =>
                        options.PendingOrderLifetimeMinutes > 0,
                    "O prazo para iniciar o pagamento deve ser positivo.")
                .Validate(
                    options =>
                        options.PaymentSessionLifetimeMinutes >= 30 &&
                        options.PaymentSessionLifetimeMinutes <= 1440,
                    "A sessão de pagamento deve durar entre 30 minutos e 24 horas.")
                .Validate(options => options.SweepIntervalSeconds > 0,
                    "O intervalo de verificação de expiração deve ser positivo.")
                .ValidateOnStart();
        }
        private static bool IsValidTimeZone(string id)
        {
            try { TimeZoneInfo.FindSystemTimeZoneById(id); return true; }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException or ArgumentException) { return false; }
        }
        public static void AddDocumentation(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(x => { x.CustomSchemaIds(n => n.FullName); });
        }
        public static void AddSecurity(this WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<UserSessionService>();
            builder.Services.TryAddSingleton<TimeProvider>(TimeProvider.System);
            builder.Services
                .AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddIdentityCookies();

            // Revoke an old application cookie on its next authenticated request.
            builder.Services.Configure<SecurityStampValidatorOptions>(options =>
                options.ValidationInterval = TimeSpan.Zero);

            builder.Services.Configure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme,
                options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = UserSessionService.AbsoluteTimeout;
                    options.SlidingExpiration = false;
                    options.Events.OnSigningIn = async context =>
                    {
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessionService>();
                        context.Properties.SetString(UserSessionService.CookieKey, (await sessions.CreateAsync()).ToString());
                        context.Properties.AllowRefresh = false;
                    };
                    options.Events.OnValidatePrincipal = async context =>
                    {
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<UserSessionService>();
                        if (Guid.TryParse(context.Properties.GetString(UserSessionService.CookieKey), out var sessionId))
                            context.HttpContext.Items[UserSessionService.CookieKey] = sessionId;
                        if (!Guid.TryParse(context.Properties.GetString(UserSessionService.CookieKey), out var id)
                            || await sessions.GetExpiryAsync(id) is null)
                        {
                            context.RejectPrincipal();
                            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                            return;
                        }
                        await SecurityStampValidator.ValidatePrincipalAsync(context);
                        // Stamp validation may request renewal; it must not extend the absolute limit.
                        context.ShouldRenew = false;
                    };
                    options.Events.OnSigningOut = async context =>
                    {
                        if (context.HttpContext.Items[UserSessionService.CookieKey] is Guid id)
                            await context.HttpContext.RequestServices.GetRequiredService<UserSessionService>().RevokeAsync(id);
                    };

                    options.Events.OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status401Unauthorized;

                        return Task.CompletedTask;
                    };

                    options.Events.OnRedirectToAccessDenied = context =>
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status403Forbidden;

                        return Task.CompletedTask;
                    };

                    if (builder.Environment.IsDevelopment())
                    {
                        options.Cookie.SameSite = SameSiteMode.Lax;
                        options.Cookie.SecurePolicy =
                            CookieSecurePolicy.SameAsRequest;
                    }
                    else
                    {
                        options.Cookie.SameSite = SameSiteMode.None;
                        options.Cookie.SecurePolicy =
                            CookieSecurePolicy.Always;

                        options.CookieManager =
                            new StaticWebAppsCookieManager();
                    }
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    AppPolicies.AdminOnly,
                    policy => policy.RequireRole(AppRoles.Admin));

                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }
        public static void AddEmailServices(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<EmailOptions>(
                builder.Configuration.GetSection(EmailOptions.SectionName));

            builder.Services.AddTransient<IEmailSender<User>, EmailSender>();
        }
        public static void AddDataContexts(this WebApplicationBuilder builder)
        {
            builder.Services.AddDbContext<AppDbContext>
                    (x => { x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")); });
            builder.Services
                    .AddIdentityCore<User>(options =>
                    {
                        options.SignIn.RequireConfirmedEmail = true;
                        options.User.RequireUniqueEmail = true;

                        options.Password.RequiredLength = 8;
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireNonAlphanumeric = false;
                    })
                    .AddRoles<IdentityRole<long>>()
                    .AddEntityFrameworkStores<AppDbContext>()
                    .AddApiEndpoints();
        }
        public static void AddServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddTransient<ICategoryHandler, CategoryHandler>();
            builder.Services.AddTransient<ITransactionHandler, TransactionHandler>();
            builder.Services.AddTransient<IVoucherHandler, VoucherHandler>();
            builder.Services.AddTransient<IOrderHandler, OrderHandler>();
            builder.Services.AddTransient<IOrderPaymentConfirmationHandler,OrderHandler>();
            builder.Services.AddTransient<IPaymentHandler, StripePaymentHandler>(); 
            builder.Services.AddTransient<IReportHandler, ReportHandler>();
            builder.Services.AddTransient<IProductHandler, ProductHandler>();
            builder.Services.AddTransient<IAdminProductHandler, ProductHandler>();
            builder.Services.AddTransient<IAdminVoucherHandler, AdminVoucherHandler>();
            builder.Services.AddTransient<IAdminUserHandler, AdminUserHandler>();
            builder.Services.AddTransient<IAdminOrderHandler, AdminOrderHandler>();
            builder.Services.AddTransient<VoucherEligibilityService>();
            builder.Services.AddSingleton<BusinessTime>();
            builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
            builder.Services.AddSingleton<IStripeClient>(services =>
            {
                var key = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiOptions>>().Value.StripeApiKey;
                return new StripeClient(string.IsNullOrWhiteSpace(key) ? null : key);
            });
            builder.Services.AddTransient<OrderExpirationService>();
            builder.Services.AddHostedService<OrderExpirationWorker>();
        }
        public static void AddCrossOrigin(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(
                options => options.AddPolicy(
                    ApiConfiguration.CorsPolicyName,
                    policy => policy
                    .WithOrigins([
                        builder.Configuration.GetValue<string>("BackendUrl") ?? string.Empty,
                        builder.Configuration.GetValue<string>("FrontendUrl") ?? string.Empty
                        ])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders(Dima.Core.Common.RequestCorrelation.HeaderName)
                    .AllowCredentials()
                    ));
        }
    }
}
