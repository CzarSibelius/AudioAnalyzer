using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Parses minimum-level strings from <see cref="AudioAnalyzer.Domain.AppLoggingSettings"/> to <see cref="LogLevel"/>.</summary>
public static class AppLoggingLevelParser
{
    /// <summary>Parses a level name; invalid or empty values become <see cref="LogLevel.Error"/>.</summary>
    public static LogLevel Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LogLevel.Error;
        }

        if (!Enum.TryParse(value.Trim(), ignoreCase: true, out LogLevel level) || level == LogLevel.None)
        {
            return LogLevel.Error;
        }

        return level;
    }
}
