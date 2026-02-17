namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Provides the currently playing media info from system or other applications (e.g. GSMTC on Windows, MPRIS on Linux).</summary>
public interface INowPlayingProvider
{
    /// <summary>Returns formatted now-playing text (e.g. "Artist - Title") or null when no session or no metadata.</summary>
    string? GetNowPlayingText();
}
