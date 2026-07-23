namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Resolves the bundled <c>mediaremote-adapter</c> artifact paths from a content root. The finalize
/// step copies <c>mediaremote-adapter.pl</c> and <c>MediaRemoteAdapter.framework</c> into
/// <c>Contents/Resources/mediaremote-adapter/</c>; the content root is that <c>Resources</c> directory.
/// </summary>
public static class MacOsMediaRemoteAdapterPaths
{
    /// <summary>Absolute path to the system Perl interpreter (a <c>com.apple.</c> platform binary).</summary>
    public const string PerlPath = "/usr/bin/perl";

    /// <summary>Subdirectory (relative to the content root) holding the adapter artifacts.</summary>
    public const string AdapterDirectoryName = "mediaremote-adapter";

    /// <summary>File name of the adapter Perl entry-point script.</summary>
    public const string ScriptFileName = "mediaremote-adapter.pl";

    /// <summary>Directory name of the helper framework (passed to the script as an argument).</summary>
    public const string FrameworkDirectoryName = "MediaRemoteAdapter.framework";

    /// <summary>Returns the adapter directory under the content root.</summary>
    /// <param name="contentRoot">Content root (the bundle <c>Contents/Resources</c> directory).</param>
    public static string GetAdapterDirectory(string contentRoot)
    {
        ArgumentNullException.ThrowIfNull(contentRoot);
        return Path.Combine(contentRoot, AdapterDirectoryName);
    }

    /// <summary>Returns the adapter Perl script path under the content root.</summary>
    /// <param name="contentRoot">Content root (the bundle <c>Contents/Resources</c> directory).</param>
    public static string GetScriptPath(string contentRoot) =>
        Path.Combine(GetAdapterDirectory(contentRoot), ScriptFileName);

    /// <summary>Returns the helper framework directory path under the content root.</summary>
    /// <param name="contentRoot">Content root (the bundle <c>Contents/Resources</c> directory).</param>
    public static string GetFrameworkPath(string contentRoot) =>
        Path.Combine(GetAdapterDirectory(contentRoot), FrameworkDirectoryName);
}
