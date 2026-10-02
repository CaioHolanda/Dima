using System.Runtime.CompilerServices;

namespace Dima.Api.Observability;

public static class OperationLoggingExtensions
{
    public static void LogOperationError(this ILogger logger, Exception exception,
        [CallerMemberName] string operation = "")
        => logger.LogError(exception, "Falha na operação {Operation}", operation);
}
