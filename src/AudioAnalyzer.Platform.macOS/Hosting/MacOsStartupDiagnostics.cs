using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>Logs macOS Core Audio tap availability during host bootstrap (shim paths per <see cref="AppContext.BaseDirectory"/>).</summary>
public sealed class MacOsStartupDiagnostics : IPlatformStartupDiagnostics
{
    private static readonly Action<ILogger, bool, bool, string?, string?, Exception?> s_coreAudioTapBootstrap =
        LoggerMessage.Define<bool, bool, string?, string?>(
            LogLevel.Information,
            new EventId(7651, "CoreAudioTapBootstrap"),
            "Core Audio tap: operating_system_supported={OsSupported}, capture_ready={CaptureReady}, base_directory={BaseDirectory}, process_directory={ProcessDirectory}");

    private readonly ILogger<MacOsStartupDiagnostics> _logger;

    /// <summary>Initializes a new instance of the <see cref="MacOsStartupDiagnostics"/> class.</summary>
    public MacOsStartupDiagnostics(ILogger<MacOsStartupDiagnostics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void LogStartup()
    {
        s_coreAudioTapBootstrap(
            _logger,
            MacOsCoreAudioTapAvailability.IsOperatingSystemSupported,
            MacOsCoreAudioTapAvailability.IsCaptureReady,
            AppContext.BaseDirectory,
            Path.GetDirectoryName(Environment.ProcessPath),
            null);
    }
}
