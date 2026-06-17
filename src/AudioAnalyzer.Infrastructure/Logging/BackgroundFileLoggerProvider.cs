using System.IO.Abstractions;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Provides file-backed loggers when <see cref="AppLoggingSettings.Enabled"/> is true.</summary>
public sealed class BackgroundFileLoggerProvider : ILoggerProvider, IDisposable
{
    private static readonly NoOpLoggerProvider s_noOp = new();

    private readonly BackgroundFileLogWriter _writer;
    private readonly LogLevel _minLevel;

    private BackgroundFileLoggerProvider(BackgroundFileLogWriter writer, LogLevel minLevel)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _minLevel = minLevel;
    }

    /// <summary>
    /// Creates a provider from settings; returns a shared no-op provider when logging is disabled.
    /// Relative log paths are resolved under <paramref name="baseDirectory"/> (writable user-data root);
    /// when null or empty, <see cref="AppContext.BaseDirectory"/> is used.
    /// </summary>
    public static ILoggerProvider Create(AppLoggingSettings? logging, IFileSystem fileSystem, string? baseDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        if (logging?.Enabled != true)
        {
            return s_noOp;
        }

        string root = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory;
        string path = LogFilePathResolver.Resolve(logging.FilePath, root);
        LogLevel minLevel = AppLoggingLevelParser.Parse(logging.MinimumLevel);
        var writer = new BackgroundFileLogWriter(fileSystem, path);
        return new BackgroundFileLoggerProvider(writer, minLevel);
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) =>
        new BackgroundFileLogger(categoryName ?? string.Empty, _minLevel, _writer);

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }
}
