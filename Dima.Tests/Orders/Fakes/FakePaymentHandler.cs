using Dima.Core.Handlers;
using Dima.Core.Requests.Payment;
using Dima.Core.Responses;

namespace Dima.Tests.Orders.Fakes;

public class FakePaymentHandler : IPaymentHandler
{
    public bool RefundWasCalled { get; private set; }

    public Task<Response<string?>> CreateSessionAsync(
        CreatePaymentSessionRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<Response<string?>> RefundAsync(
        string externalReference)
    {
        RefundWasCalled = true;

        return Task.FromResult(
            new Response<string?>(
                "re_test_refund",
                200,
                "Refund fake criado"));
    }
}