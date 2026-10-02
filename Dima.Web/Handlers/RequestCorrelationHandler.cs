using Dima.Core.Common;

namespace Dima.Web.Handlers;

public sealed class RequestCorrelationHandler(ILogger<RequestCorrelationHandler> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = RequestCorrelation.Create();
        request.Headers.Remove(RequestCorrelation.HeaderName);
        request.Headers.Add(RequestCorrelation.HeaderName, correlationId);
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId
        });
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("API retornou {StatusCode}; correlação {CorrelationId}",
                    (int)response.StatusCode, correlationId);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Falha ao chamar a API; correlação {CorrelationId}", correlationId);
            throw;
        }
    }
}
