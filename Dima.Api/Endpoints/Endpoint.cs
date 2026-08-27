using Dima.Api.Common.Api;
using Dima.Api.Endpoints.Admin;
using Dima.Api.Endpoints.Categories;
using Dima.Api.Endpoints.Identity;
using Dima.Api.Endpoints.Orders;
using Dima.Api.Endpoints.Products;
using Dima.Api.Endpoints.Reports;
using Dima.Api.Endpoints.Stripe;
using Dima.Api.Endpoints.Transactions;
using Dima.Api.Endpoints.Users;
using Dima.Api.Endpoints.Vouchers;
using Dima.Api.Models;
using Dima.Core.Requests.Categories;
using Dima.Core.Security;

namespace Dima.Api.Endpoints;

public static class Endpoint
{
    // Extension Method
    public static void MapEndpoints(this WebApplication app)
    {
        var endpoint = app
            .MapGroup("/api");

        endpoint.MapGroup("/")
            .WithTags("Health Check")
            .AllowAnonymous()
            .MapGet("/", () => new { message = "OK" });

        endpoint.MapGroup("v1/categories")
            .WithTags("Categories")
            .RequireAuthorization()
            .MapEndpoint<CreateCategoryEndpoint>()
            .MapEndpoint<UpdateCategoryEndpoint>()
            .MapEndpoint<DeleteCategoryEndpoint>()
            .MapEndpoint<GetCategoryByIdEndpoint>()
            .MapEndpoint<GetAllCategoriesEndpoint>();

        endpoint.MapGroup("v1/transactions")
            .WithTags("Transactions")
            .RequireAuthorization()
            .MapEndpoint<CreateTransactionEndpoint>()
            .MapEndpoint<UpdateTransactionEndpoint>()
            .MapEndpoint<DeleteTransactionEndpoint>()
            .MapEndpoint<GetTransactionByIdEndpoint>()
            .MapEndpoint<GetTransactionsByPeriodEndpoint>();

        endpoint.MapGroup("v1/products")
            .WithTags("Products")
            .AllowAnonymous()
            .MapEndpoint<GetAllProductsEndpoint>()
            .MapEndpoint<GetProductBySlugEndpoint>();

        endpoint.MapGroup("v1/admin/products")
            .WithTags("Admin - Products")
            .RequireAuthorization("AdminOnly")
            .MapEndpoint<GetAllAdminProductsEndpoint>()
            .MapEndpoint<GetAdminProductByIdEndpoint>()
            .MapEndpoint<CreateProductEndpoint>()
            .MapEndpoint<UpdateProductEndpoint>()
            .MapEndpoint<DeactivateProductEndpoint>()
            .MapEndpoint<ActivateProductEndpoint>();

        endpoint.MapGroup("v1/admin/vouchers")
            .WithTags("Admin - Vouchers")
            .RequireAuthorization("AdminOnly")
            .MapEndpoint<CreateVoucherEndpoint>()
            .MapEndpoint<GetAllAdminVouchersEndpoint>()
            .MapEndpoint<GetAdminVoucherByIdEndpoint>()
            .MapEndpoint<UpdateVoucherEndpoint>()
            .MapEndpoint<DeactivateVoucherEndpoint>()
            .MapEndpoint<ActivateVoucherEndpoint>();

        endpoint.MapGroup("v1/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization("AdminOnly")
            .MapEndpoint<GetAllAdminUsersEndpoint>()
            .MapEndpoint<SearchUsersEndpoint>()
            .MapEndpoint<ActivateUserEndpoint>()
            .MapEndpoint<DeactivateUserEndpoint>();

        endpoint.MapGroup("v1/admin/orders")
            .WithTags("Admin - Orders")
            .RequireAuthorization("AdminOnly")
            .MapEndpoint<GetAllAdminOrdersEndpoint>();

        endpoint.MapGroup("v1/vouchers")
            .WithTags("Vouchers")
            .RequireAuthorization()
            .MapEndpoint<GetVoucherByCodeEndpoint>();

        endpoint.MapGroup("v1/orders")
            .WithTags("Orders")
            .RequireAuthorization()
            .MapEndpoint<GetAllOrdersEndpoint>()
            .MapEndpoint<GetOrderByNumberEndpoint>()
            .MapEndpoint<CreateOrderEndpoint>()
            .MapEndpoint<CancelOrderEndpoint>()
            .MapEndpoint<PayOrderEndpoint>()
            .MapEndpoint<RefundOrderEndpoint>();

        var stripe = endpoint
            .MapGroup("v1/payments/stripe")
            .WithTags("Payments - Stripe");

        stripe
            .MapGroup("/")
            .RequireAuthorization()
            .MapEndpoint<CreateSessionEndpoint>();

        stripe
            .MapEndpoint<WebhookEndpoint>();

        endpoint.MapGroup("v1/identity")
            .WithTags("Identity")
            .MapIdentityApi<User>();

        endpoint.MapGroup("v1/identity")
            .WithTags("Identity")
            .MapEndpoint<LoginEndpoint>()
            .MapEndpoint<RegisterEndpoint>()
            .MapEndpoint<LogoutEndpoint>()
            .MapEndpoint<GetRolesEndpoint>()
            .MapEndpoint<ForgotPasswordEndpoint>()
            .MapEndpoint<ResetPasswordEndpoint>();

        endpoint.MapGroup("v1/admin")
            .WithTags("Admin")
            .MapEndpoint<ValidateAdminEndpoint>();

        endpoint.MapGroup("v1/reports")
            .WithTags("Reports")
            .RequireAuthorization()
            .MapEndpoint<GetExpensesByCategoryEndpoint>()
            .MapEndpoint<GetFinancialSummaryEndpoint>()
            .MapEndpoint<GetIncomesAndExpensesEndpoint>()
            .MapEndpoint<GetIncomesByCategoryEndpoint>();

    }
    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
        where TEndpoint:IEndpoint
    {
        TEndpoint.Map(app);
            return app;
    }
}

