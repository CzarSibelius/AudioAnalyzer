using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Reports whether the <c>mediaremote-adapter</c> now-playing mechanism can run on this host:
/// the system Perl interpreter, the bundled adapter script, and the helper framework must all be
/// present. Used by the macOS DI factory to choose the adapter provider or fall back to
/// <c>NullNowPlayingProvider</c>. Path resolution uses <see cref="MacOsMediaRemoteAdapterPaths"/>.
/// </summary>
public sealed class MacOsMediaRemoteAdapterAvailability
{
    private readonly IHostContentLocator _contentLocator;
    private readonly Func<string, bool> _fileExists;
    private readonly Func<string, bool> _directoryExists;

    /// <summary>Initializes a new instance of the <see cref="MacOsMediaRemoteAdapterAvailability"/> class.</summary>
    /// <param name="contentLocator">Locator that resolves the bundle content root.</param>
    public MacOsMediaRemoteAdapterAvailability(IHostContentLocator contentLocator)
        : this(contentLocator, File.Exists, Directory.Exists)
    {
    }

    /// <summary>Initializes a new instance with injectable existence probes (tests).</summary>
    /// <param name="contentLocator">Locator that resolves the bundle content root.</param>
    /// <param name="fileExists">Predicate that reports whether a file exists.</param>
    /// <param name="directoryExists">Predicate that reports whether a directory exists.</param>
    public MacOsMediaRemoteAdapterAvailability(
        IHostContentLocator contentLocator,
        Func<string, bool> fileExists,
        Func<string, bool> directoryExists)
    {
        _contentLocator = contentLocator ?? throw new ArgumentNullException(nameof(contentLocator));
        _fileExists = fileExists ?? throw new ArgumentNullException(nameof(fileExists));
        _directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
    }

    /// <summary>True when the artifacts are resolvable and present so the provider can spawn the adapter.</summary>
    public bool IsAvailable => TryResolvePaths(out _, out _);

    /// <summary>
    /// Attempts to resolve the adapter script and framework paths, returning true only when the
    /// content root is known and all required artifacts (Perl, script, framework) exist.
    /// </summary>
    /// <param name="scriptPath">Resolved adapter script path when available.</param>
    /// <param name="frameworkPath">Resolved helper framework path when available.</param>
    public bool TryResolvePaths(out string scriptPath, out string frameworkPath)
    {
        scriptPath = string.Empty;
        frameworkPath = string.Empty;

        if (!_contentLocator.TryGetContentRoots(out string contentRoot, out _))
        {
            return false;
        }

        string resolvedScript = MacOsMediaRemoteAdapterPaths.GetScriptPath(contentRoot);
        string resolvedFramework = MacOsMediaRemoteAdapterPaths.GetFrameworkPath(contentRoot);

        if (!_fileExists(MacOsMediaRemoteAdapterPaths.PerlPath) ||
            !_fileExists(resolvedScript) ||
            !_directoryExists(resolvedFramework))
        {
            return false;
        }

        scriptPath = resolvedScript;
        frameworkPath = resolvedFramework;
        return true;
    }
}
