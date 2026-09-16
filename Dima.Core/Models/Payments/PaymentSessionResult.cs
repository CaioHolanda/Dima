using Dima.Core.Enums;

namespace Dima.Core.Models.Payments;

public sealed class PaymentSessionResult
{
    public required string SessionId { get; init; }

    public required string RedirectUrl { get; init; } 

    public DateTimeOffset ExpiresAt { get; init; }

    public EPaymentGateway Gateway { get; init; }
}