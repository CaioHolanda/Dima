using System.Security.Cryptography;
using System.Text;

namespace Dima.Core.Common;

public static class RequestCorrelation
{
    public const string HeaderName = "X-Correlation-ID";
    public const string StripeMetadataKey = "correlation_id";

    // A bounded, normalized value avoids arbitrary client input in logs and headers.
    public static string? Normalize(string? value)
        => Guid.TryParseExact(value, "N", out var id) && id != Guid.Empty
            ? id.ToString("N")
            : Guid.TryParseExact(value, "D", out id) && id != Guid.Empty
                ? id.ToString("N") : null;

    public static string Create() => Guid.NewGuid().ToString("N");

    // Stripe requires identical parameters when an idempotency key is retried.
    // This identifier links checkout logs to its webhook without changing metadata
    // when the HTTP request's CorrelationId changes between retries.
    public static string ForPaymentAttempt(string idempotencyKey)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey)).AsSpan(0, 16))
            .ToLowerInvariant();
}
