using Dima.Core.Enums;
using Dima.Core.Handlers;
using Dima.Core.Models.Payments;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;

namespace Dima.Tests.Orders.Fakes;

public class FakePaymentHandler : IPaymentHandler
{
    public bool RefundWasCalled { get; private set; }

    public string? LastExternalReference { get; private set; }

    public string? LastIdempotencyKey { get; private set; }
    public bool CloseSessionShouldSucceed { get; set; } = true;

    public bool CloseSessionWasCalled { get; private set; }

    public Task<Response<string?>> RefundAsync(
        string externalReference,
        string idempotencyKey)
    {
        RefundWasCalled = true;
        LastExternalReference = externalReference;
        LastIdempotencyKey = idempotencyKey;

        return Task.FromResult(
            new Response<string?>(
                "re_test_refund",
                200,
                "Refund fake criado"));
    }

    public Task<Response<PaymentSessionResult?>> CreateSessionAsync(CreatePaymentSessionRequest request)
    {
        return Task.FromResult(
            new Response<PaymentSessionResult?>(
                new PaymentSessionResult
                {
                    SessionId = "test-session",
                    RedirectUrl = "https://test.local/checkout",
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
                    Gateway = EPaymentGateway.Stripe
                }));
    }
    public Task<Response<bool>> CloseSessionAsync(string sessionId)
    {
        CloseSessionWasCalled = true;

        return Task.FromResult(
            CloseSessionShouldSucceed
                ? new Response<bool>(
                    true,
                    200,
                    "Sessão fake encerrada")
                : new Response<bool>(
                    false,
                    409,
                    "Sessão fake não pôde ser encerrada"));
    }
}