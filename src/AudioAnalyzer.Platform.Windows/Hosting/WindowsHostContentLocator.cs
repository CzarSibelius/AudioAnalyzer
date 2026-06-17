using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Hosting;

/// <summary>Windows host content locator: uses the base directory for content and writable data (no bundle).</summary>
public sealed class WindowsHostContentLocator : IHostContentLocator
{
    /// <inheritdoc />
    public bool TryGetContentRoots(out string contentRoot, out string writableRoot)
    {
        contentRoot = string.Empty;
        writableRoot = string.Empty;
        return false;
    }
}
