using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>
/// Resolves content/writable roots for a macOS <c>.app</c> bundle: read-only content under
/// <c>Contents/Resources</c> and writable data under Application Support.
/// </summary>
public sealed class MacOsHostContentLocator : IHostContentLocator
{
    /// <inheritdoc />
    public bool TryGetContentRoots(out string contentRoot, out string writableRoot)
    {
        contentRoot = string.Empty;
        writableRoot = string.Empty;

        string? processPath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(processPath) ||
            !processPath.Contains(".app/Contents/MacOS/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? macOsDir = Path.GetDirectoryName(processPath);
        string? contentsDir = macOsDir is not null ? Path.GetDirectoryName(macOsDir) : null;
        if (contentsDir is null)
        {
            return false;
        }

        string resourcesDirectory = Path.Combine(contentsDir, "Resources");
        if (!Directory.Exists(resourcesDirectory))
        {
            return false;
        }

        contentRoot = resourcesDirectory;
        writableRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioAnalyzer");
        return true;
    }
}
