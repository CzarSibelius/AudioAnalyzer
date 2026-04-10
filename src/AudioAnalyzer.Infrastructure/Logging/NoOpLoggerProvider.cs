using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Provides no-op loggers for all categories when file logging is disabled.</summary>
internal sealed class NoOpLoggerProvider : ILoggerProvider
{
    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => DiscardLogger.Instance;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
