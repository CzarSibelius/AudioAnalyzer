using System.Text.Json.Serialization;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Now-playing fields from a <c>mediaremote-adapter</c> <c>stream</c> JSON line payload. Only the
/// title/artist/album subset is mapped; the adapter emits many more keys we do not render.
/// </summary>
public sealed class MediaRemoteAdapterPayload
{
    /// <summary>Track title, or null when the payload omits it (no reporting player).</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Artist name, or null when the payload omits it.</summary>
    [JsonPropertyName("artist")]
    public string? Artist { get; init; }

    /// <summary>Album name, or null when the payload omits it.</summary>
    [JsonPropertyName("album")]
    public string? Album { get; init; }
}
