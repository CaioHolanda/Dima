using Dima.Api.Common.Api;
using Dima.Api.Data;
using Dima.Api.Handlers;
using Dima.Api.Services;
using Dima.Core.Handlers;
using Dima.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dima.Tests.Orders;

public class PaymentArchitectureTests
{
    [Fact]
    public void Api_resolves_payment_consumers_with_validated_dependencies()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DimaDt12Validation;Trusted_Connection=True"));
        builder.Services.AddIdentityCore<Dima.Api.Models.User>()
            .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole<long>>()
            .AddEntityFrameworkStores<AppDbContext>();
        builder.AddServices();

        using var provider = builder.Services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.IsType<StripePaymentHandler>(scope.ServiceProvider.GetRequiredService<IPaymentHandler>());
        Assert.IsType<OrderHandler>(scope.ServiceProvider.GetRequiredService<IOrderHandler>());
        Assert.IsType<OrderHandler>(scope.ServiceProvider.GetRequiredService<IOrderPaymentConfirmationHandler>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<OrderExpirationService>());
    }

    [Fact]
    public void SqlServer_model_matches_migrations_and_gateway_is_optional()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=DimaDt12Validation;Trusted_Connection=True")
            .Options;
        using var context = new AppDbContext(options);

        Assert.True(context.Model.FindEntityType(typeof(Order))!
            .FindProperty(nameof(Order.Gateway))!.IsNullable);
        Assert.Contains("20260914202811_MakeOrderGatewayNullable", context.Database.GetMigrations());
        Assert.False(context.Database.HasPendingModelChanges());
    }
}