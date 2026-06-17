using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Hosting;

/// <summary>Windows host has no extra startup diagnostics; this is a no-op.</summary>
public sealed class WindowsStartupDiagnostics : IPlatformStartupDiagnostics
{
    /// <inheritdoc />
    public void LogStartup()
    {
        // No Windows-specific startup diagnostics.
    }
}
