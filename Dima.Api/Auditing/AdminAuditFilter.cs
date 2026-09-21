using System.Security.Claims;
using System.Text.Json;
using Dima.Api.Data;
using Dima.Api.Models;
using Dima.Core.Security;
using Microsoft.EntityFrameworkCore;

namespace Dima.Api.Auditing;

public sealed class AdminAuditFilter(
    IServiceScopeFactory scopeFactory,
    ILogger<AdminAuditFilter> logger, TimeProvider timeProvider) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext invocation, EndpointFilterDelegate next)
    {
        var http = invocation.HttpContext;
        var path = http.Request.Path.Value ?? "";
        if (!http.User.IsInRole(AppRoles.Admin)
            || !(HttpMethods.IsPost(http.Request.Method) || HttpMethods.IsPut(http.Request.Method)
                || HttpMethods.IsPatch(http.Request.Method) || HttpMethods.IsDelete(http.Request.Method)))
            return await next(invocation);

        var target = path.StartsWith("/api/v1/admin/products", StringComparison.OrdinalIgnoreCase) ? "Product"
            : path.StartsWith("/api/v1/admin/vouchers", StringComparison.OrdinalIgnoreCase) ? "Voucher"
            : path.StartsWith("/api/v1/admin/users", StringComparison.OrdinalIgnoreCase) ? "User"
            : path.StartsWith("/api/v1/admin/orders", StringComparison.OrdinalIgnoreCase)
                || (path.StartsWith("/api/v1/orders/", StringComparison.OrdinalIgnoreCase)
                    && (path.EndsWith("/refund", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith("/cancel", StringComparison.OrdinalIgnoreCase))) ? "Order" : null;
        if (target is null)
            return await next(invocation);

        var log = new AdminAuditLog
        {
            OccurredAtUtc = timeProvider.GetUtcNow(),
            ActorId = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.Identity?.Name ?? "unknown",
            ActorName = http.User.Identity?.Name,
            TargetType = target,
            TargetId = long.TryParse(http.Request.RouteValues["id"]?.ToString(), out var id) ? id : null,
            Operation = $"{http.Request.Method} {http.GetEndpoint()?.DisplayName ?? path}",
            StatusCode = 500
        };
        log.BeforeJson = await TrySnapshotAsync(target, log.TargetId);
        try
        {
            var result = await next(invocation);
            log.StatusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? http.Response.StatusCode;
            log.Succeeded = log.StatusCode is >= 200 and < 300;
            if (log.TargetId is null && result is IValueHttpResult valueResult)
            {
                var data = valueResult.Value?.GetType().GetProperty("Data")?.GetValue(valueResult.Value);
                if (data?.GetType().GetProperty("Id")?.GetValue(data) is long createdId)
                    log.TargetId = createdId;
            }
            return result;
        }
        finally
        {
            // A separate context cannot accidentally save changes left by a failed handler.
            try
            {
                log.AfterJson = await TrySnapshotAsync(target, log.TargetId);
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.AdminAuditLogs.Add(log);
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not persist administrative audit for {Operation}, target {TargetId}", log.Operation, log.TargetId);
            }
        }
    }

    private async Task<string?> TrySnapshotAsync(string target, long? id)
    {
        try
        {
            return await SnapshotAsync(target, id);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read audit snapshot for {TargetType} {TargetId}", target, id);
            return null;
        }
    }

    private async Task<string?> SnapshotAsync(string target, long? id)
    {
        if (id is null) return null;
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        object? snapshot = target switch
        {
            "Product" => await db.Products.AsNoTracking().Where(x => x.Id == id).Select(x => new
            { x.Id, x.Title, x.Slug, x.Description, x.Price, x.IsActive, x.AccessDurationMonths }).SingleOrDefaultAsync(),
            "Voucher" => await db.Vouchers.AsNoTracking().Where(x => x.Id == id).Select(x => new
            { x.Id, x.Code, x.Title, x.Description, x.DiscountType, x.Value, x.StartsAt, x.EndsAt,
                x.MaxTotalUses, x.MaxUsesPerUser, x.AssignedUserId, x.ProductId, x.IsActive }).SingleOrDefaultAsync(),
            "User" => await db.Users.AsNoTracking().Where(x => x.Id == id).Select(x => new
            { x.Id, x.Email, x.LockoutEnabled, x.LockoutEnd }).SingleOrDefaultAsync(),
            "Order" => await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => new
            { x.Id, x.Status, x.UpdatedAt, x.ExpiresAt, x.ExpiredAt, x.PaymentSessionId,
                x.PaidAt, x.ExternalReference, x.Total, x.Gateway, x.RefundReference, x.RefundFailureReason,
                x.RefundedAt, x.RefundReason, x.RefundReasonDetails, x.AccessStartsAt, x.AccessEndsAt }).SingleOrDefaultAsync(),
            _ => null
        };
        return snapshot is null ? null : JsonSerializer.Serialize(snapshot);
    }
}

