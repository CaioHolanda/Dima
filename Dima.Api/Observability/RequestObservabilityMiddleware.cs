using System.Diagnostics;
using Dima.Core.Common;

namespace Dima.Api.Observability;

public sealed class RequestObservabilityMiddleware(RequestDelegate next,
    ILogger<RequestObservabilityMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var values = context.Request.Headers[RequestCorrelation.HeaderName];
        var correlationId = (values.Count == 1 ? RequestCorrelation.Normalize(values[0]) : null)
            ?? RequestCorrelation.Create();
        context.Items[RequestCorrelation.HeaderName] = correlationId;
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[RequestCorrelation.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["TraceId"] = traceId,
            ["RequestMethod"] = context.Request.Method,
            ["RequestPath"] = context.Request.Path.Value
        });
        var started = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Exceção não tratada na requisição");
            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            await Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
                title: "Não foi possível concluir a operação.",
                extensions: new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["traceId"] = traceId
                }).ExecuteAsync(context);
        }
        finally
        {
            logger.LogInformation("Requisição concluída: {StatusCode} em {ElapsedMilliseconds} ms",
                context.Response.StatusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        }
    }
}
