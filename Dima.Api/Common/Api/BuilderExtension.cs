using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Models;
using Dima.Api.Services.Email;
using Dima.Core;
using Dima.Core.Handlers;
using Dima.Core.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dima.Api.Configuration;
using CoreConfiguration = Dima.Core.Configuration;
using Stripe;

namespace Dima.Api.Common.Api
{
    public static class BuilderExtension
    {
        public static void AddConfiguration(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<InitialAdminOptions>
                (builder.Configuration.GetSection(InitialAdminOptions.SectionName));
            CoreConfiguration.ConnectionString=builder.Configuration
                .GetConnectionString("DefaultConnection") ?? string.Empty;
            CoreConfiguration.BackendUrl=builder.Configuration
                .GetValue<string>("BackendUrl")?? string.Empty;
            CoreConfiguration.FrontendUrl=builder.Configuration
                .GetValue<string>("FrontendUrl")?? string.Empty;
            ApiConfiguration.StripeApiKey = builder.Configuration
                .GetValue<string>("StripeApiKey") ?? string.Empty;
            StripeConfiguration.ApiKey = ApiConfiguration.StripeApiKey;
        }
        public static void AddDocumentation(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(x => { x.CustomSchemaIds(n => n.FullName); });
        }
        public static void AddSecurity(this WebApplicationBuilder builder)
        {
            builder.Services
                .AddAuthentication(IdentityConstants.ApplicationScheme)
                .AddIdentityCookies();

            builder.Services.Configure<CookieAuthenticationOptions>(
                IdentityConstants.ApplicationScheme,
                options =>
                {
                    options.Cookie.HttpOnly = true;

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
                    }
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    AppPolicies.AdminOnly,
                    policy => policy.RequireRole(AppRoles.Admin));
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
                    (x => { x.UseSqlServer(CoreConfiguration.ConnectionString); });
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
            builder.Services.AddTransient<IProductHandler, ProductHandler>();
            builder.Services.AddTransient<IVoucherHandler, VoucherHandler>();
            builder.Services.AddTransient<IOrderHandler, OrderHandler>();
            builder.Services.AddTransient<IStripeHandler, StripeHanlder>();
            builder.Services.AddTransient<IReportHandler, ReportHandler>();
        }
        public static void AddCrossOrigin(this WebApplicationBuilder builder)
        {
            builder.Services.AddCors(
                options => options.AddPolicy(
                    ApiConfiguration.CorsPolicyName,
                    policy => policy
                    .WithOrigins([
                        CoreConfiguration.BackendUrl,
                        CoreConfiguration.FrontendUrl
                        ])
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    ));
        }
    }
}
