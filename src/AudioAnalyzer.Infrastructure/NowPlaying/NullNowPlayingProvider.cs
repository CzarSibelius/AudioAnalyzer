using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure.NowPlaying;

/// <summary>Now-playing provider that always returns null. Used on non-Windows platforms or as fallback.</summary>
public sealed class NullNowPlayingProvider : INowPlayingProvider
{
    /// <inheritdoc />
    public NowPlayingInfo? GetNowPlaying() => null;
}
