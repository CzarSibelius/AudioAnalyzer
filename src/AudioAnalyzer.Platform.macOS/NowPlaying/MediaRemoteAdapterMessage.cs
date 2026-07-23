using System.Text.Json.Serialization;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Envelope for a single <c>mediaremote-adapter</c> <c>stream</c> stdout line. With
/// <c>--no-diff</c> each line is a complete state (<see cref="Diff"/> is false), so no diff-merge
/// state machine is needed; <see cref="Payload"/> is empty when no application is reporting media.
/// </summary>
public sealed class MediaRemoteAdapterMessage
{
    /// <summary>Message type; the <c>stream</c> command emits <c>"data"</c> for now-playing lines.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>True when the payload is a partial diff; always false under <c>--no-diff</c>.</summary>
    [JsonPropertyName("diff")]
    public bool Diff { get; init; }

    /// <summary>Now-playing fields, or null/empty when no application is reporting media.</summary>
    [JsonPropertyName("payload")]
    public MediaRemoteAdapterPayload? Payload { get; init; }
}
