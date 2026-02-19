namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Provides the currently playing media info from system or other applications (e.g. GSMTC on Windows, MPRIS on Linux).</summary>
public interface INowPlayingProvider
{
    /// <summary>Returns structured now-playing info (Title, Artist, Album) or null when no session or no metadata. Callers choose what to display (e.g. <see cref="NowPlayingInfo.ToDisplayString"/>).</summary>
    NowPlayingInfo? GetNowPlaying();
}
