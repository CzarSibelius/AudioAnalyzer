namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Emits platform-specific startup diagnostics during host bootstrap (e.g. macOS Core Audio tap
/// availability). Implemented per platform (other platforms are no-ops) and injected so the shared
/// host entry point does not branch on the operating system. Implementations obtain their logger
/// via dependency injection.
/// </summary>
public interface IPlatformStartupDiagnostics
{
    /// <summary>Logs platform startup diagnostics.</summary>
    void LogStartup();
}
