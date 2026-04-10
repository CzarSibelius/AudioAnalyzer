namespace AudioAnalyzer.Infrastructure.Logging;

/// <summary>Resolves configured log file paths relative to the application base directory.</summary>
public static class LogFilePathResolver
{
    /// <summary>
    /// Returns an absolute path. Empty <paramref name="filePathRelativeOrAbsolute"/> uses <c>logs/audioanalyzer.log</c> under <paramref name="baseDirectory"/>.
    /// </summary>
    public static string Resolve(string? filePathRelativeOrAbsolute, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePathRelativeOrAbsolute))
        {
            return Path.GetFullPath(Path.Combine(baseDirectory, "logs", "audioanalyzer.log"));
        }

        string trimmed = filePathRelativeOrAbsolute.Trim();
        if (Path.IsPathRooted(trimmed))
        {
            return Path.GetFullPath(trimmed);
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, trimmed));
    }
}
