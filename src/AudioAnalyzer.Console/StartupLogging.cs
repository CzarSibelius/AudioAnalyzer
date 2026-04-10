using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>Source-generated style log delegates for host startup (top-level <c>Program</c> has no instance logger).</summary>
internal static class StartupLogging
{
    private static readonly Action<ILogger, Exception?> s_applicationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(7650, "ApplicationStarted"),
        "Application started.");

    /// <summary>Writes an Information entry when the app is about to run the main loop.</summary>
    public static void LogApplicationStarted(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        s_applicationStarted(logger, null);
    }
}
