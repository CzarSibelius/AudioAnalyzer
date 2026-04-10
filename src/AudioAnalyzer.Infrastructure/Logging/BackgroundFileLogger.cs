using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Writes formatted entries to <see cref="BackgroundFileLogWriter"/>.</summary>
internal sealed class BackgroundFileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly LogLevel _minLevel;
    private readonly BackgroundFileLogWriter _writer;

    public BackgroundFileLogger(string categoryName, LogLevel minLevel, BackgroundFileLogWriter writer)
    {
        _categoryName = categoryName ?? string.Empty;
        _minLevel = minLevel;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _minLevel;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);
        string message = formatter(state, exception);
        var sb = new StringBuilder(256);
        sb.Append(DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        sb.Append(" | ");
        sb.Append(logLevel);
        sb.Append(" | ");
        sb.Append(_categoryName);
        sb.Append(" | ");
        sb.AppendLine(message);
        if (exception != null)
        {
            sb.AppendLine(exception.ToString());
        }

        sb.AppendLine();
        _ = _writer.TryEnqueue(sb.ToString());
    }
}
