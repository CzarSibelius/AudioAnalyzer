using AudioAnalyzer.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>Source-generated style log delegates for host startup (top-level <c>Program</c> has no instance logger).</summary>
internal static class StartupLogging
{
    private static readonly Action<ILogger, Exception?> s_applicationStarted = LoggerMessage.Define(
        LogLevel.Information,
        new EventId(7650, "ApplicationStarted"),
        "Application started.");

    private static readonly Action<ILogger, int, int, int, Exception?> s_featureCapabilitiesSummary =
        LoggerMessage.Define<int, int, int>(
            LogLevel.Information,
            new EventId(7652, "FeatureCapabilitiesSummary"),
            "Feature capabilities: {Available} available, {Unavailable} unavailable, {NotApplicable} n/a");

    private static readonly Action<ILogger, string, FeatureAvailability, string, Exception?> s_featureCapability =
        LoggerMessage.Define<string, FeatureAvailability, string>(
            LogLevel.Information,
            new EventId(7653, "FeatureCapability"),
            "Feature capability {Id}: {Availability} ({Detail})");

    /// <summary>Writes an Information entry when the app is about to run the main loop.</summary>
    public static void LogApplicationStarted(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        s_applicationStarted(logger, null);
    }

    /// <summary>Writes one summary Information line plus one Information line per feature capability.</summary>
    public static void LogFeatureCapabilities(ILogger logger, IReadOnlyList<FeatureCapabilityStatus> statuses)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(statuses);

        int available = 0;
        int unavailable = 0;
        int notApplicable = 0;
        foreach (var status in statuses)
        {
            switch (status.Availability)
            {
                case FeatureAvailability.Available:
                    available++;
                    break;
                case FeatureAvailability.Unavailable:
                    unavailable++;
                    break;
                default:
                    notApplicable++;
                    break;
            }
        }

        s_featureCapabilitiesSummary(logger, available, unavailable, notApplicable, null);
        foreach (var status in statuses)
        {
            s_featureCapability(logger, status.Id, status.Availability, status.Detail, null);
        }
    }
}
