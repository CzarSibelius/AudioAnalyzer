using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Discards all log entries (used when file logging is off).</summary>
internal sealed class DiscardLogger : ILogger
{
    public static readonly ILogger Instance = new DiscardLogger();

    private DiscardLogger()
    {
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => NullScope.Instance;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => false;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}
