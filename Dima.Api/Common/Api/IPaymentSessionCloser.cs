using Dima.Core.Responses;

namespace Dima.Api.Common.Api;

public interface IPaymentSessionCloser
{
    Task<Response<bool>> CloseAsync(string sessionId);
}