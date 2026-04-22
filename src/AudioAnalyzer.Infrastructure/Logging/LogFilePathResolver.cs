using System.Globalization;

namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Resolves configured log file paths relative to the application base directory.</summary>
public static class LogFilePathResolver
{
    private const string ProcessIdPlaceholder = "{ProcessId}";
    private const string DefaultRelativeTemplate = "logs/audioanalyzer-{ProcessId}.log";

    /// <summary>
    /// Returns an absolute path. Expands <see cref="ProcessIdPlaceholder"/> with <see cref="Environment.ProcessId"/>.
    /// Empty <paramref name="filePathRelativeOrAbsolute"/> uses the default relative template <c>logs/audioanalyzer-{ProcessId}.log</c> under <paramref name="baseDirectory"/> (placeholder expanded at resolve time).
    /// </summary>
    public static string Resolve(string? filePathRelativeOrAbsolute, string baseDirectory)
    {
        string template = string.IsNullOrWhiteSpace(filePathRelativeOrAbsolute)
            ? DefaultRelativeTemplate
            : filePathRelativeOrAbsolute.Trim();

        string expanded = ExpandPlaceholders(template);
        if (Path.IsPathRooted(expanded))
        {
            return Path.GetFullPath(expanded);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, expanded));
    }

    private static string ExpandPlaceholders(string path) =>
        path.Replace(ProcessIdPlaceholder, Environment.ProcessId.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
}
