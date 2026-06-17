namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Resolves platform-specific content and writable-data roots for the host (e.g. a macOS
/// <c>.app</c> bundle places read-only content under <c>Contents/Resources</c> and writable data
/// under Application Support). Implemented per platform (other platforms return false to use the
/// base directory) and injected so path resolution does not branch on the operating system.
/// </summary>
public interface IHostContentLocator
{
    /// <summary>
    /// Attempts to resolve platform-specific roots. Returns true and sets both roots when the
    /// platform overrides the defaults; otherwise returns false and the caller uses the base directory.
    /// </summary>
    bool TryGetContentRoots(out string contentRoot, out string writableRoot);
}
