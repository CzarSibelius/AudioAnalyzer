namespace AudioAnalyzer.Domain;

/// <summary>
/// File logging options persisted in <c>appsettings.json</c> under <c>Logging</c>. See ADR-0076.
/// </summary>
public sealed class AppLoggingSettings
{
    /// <summary>When false, file logging is off and <see cref="MinimumLevel"/> is ignored.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Log file path. Relative paths are resolved under the application base directory.
    /// When null or whitespace, defaults to <c>logs/audioanalyzer.log</c> under the base directory.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Minimum level written to the file when <see cref="Enabled"/> is true.
    /// Names match Microsoft.Extensions.Logging.LogLevel (e.g. Error, Warning, Information, Debug, Trace).
    /// Invalid or missing values default to Error.
    /// </summary>
    public string? MinimumLevel { get; set; } = "Error";
}
