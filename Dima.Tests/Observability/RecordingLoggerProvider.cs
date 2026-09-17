using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Dima.Tests.Observability;

public sealed record LogEntry(LogLevel Level, Exception? Exception, string Message,
    IReadOnlyDictionary<string, object?> Properties);

public sealed class RecordingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
    public ConcurrentQueue<LogEntry> Entries { get; } = new();
    public ILogger CreateLogger(string categoryName) => new Logger(this, categoryName);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;
    public void Dispose() { }

    private sealed class Logger(RecordingLoggerProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => owner._scopes.Push(state);
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = new Dictionary<string, object?> { ["Category"] = category };
            owner._scopes.ForEachScope((scope, target) => AddProperties(scope, target), properties);
            AddProperties(state, properties);
            owner.Entries.Enqueue(new(logLevel, exception, formatter(state, exception), properties));
        }

        private static void AddProperties(object? value, Dictionary<string, object?> target)
        {
            if (value is IEnumerable<KeyValuePair<string, object?>> fields)
                foreach (var field in fields) target[field.Key] = field.Value;
        }
    }
}
