using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.NowPlaying;

/// <summary>
/// Parses a single <c>mediaremote-adapter</c> <c>stream --no-diff</c> stdout line into a
/// <see cref="NowPlayingInfo"/>. An empty payload (no reporting player) maps to <c>null</c>.
/// </summary>
public static class MediaRemoteAdapterPayloadParser
{
    /// <summary>
    /// Attempts to parse a JSON line. Returns true when the line is a well-formed <c>data</c>
    /// message (even with an empty payload); <paramref name="info"/> is the mapped now-playing info
    /// or <c>null</c> when no track is reported. Non-JSON or non-data lines return false.
    /// </summary>
    /// <param name="line">A single stdout line from the adapter.</param>
    /// <param name="info">The mapped now-playing info, or null when the payload reports no track.</param>
    public static bool TryParse(string? line, out NowPlayingInfo? info)
    {
        info = null;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        MediaRemoteAdapterMessage? message;
        try
        {
            message = JsonSerializer.Deserialize(line, MediaRemoteAdapterJsonContext.Default.MediaRemoteAdapterMessage);
        }
        catch (JsonException)
        {
            return false;
        }

        if (message is null ||
            !string.Equals(message.Type, "data", StringComparison.Ordinal))
        {
            return false;
        }

        info = MapPayload(message.Payload);
        return true;
    }

    /// <summary>Maps a payload to <see cref="NowPlayingInfo"/>, returning null when no track fields are set.</summary>
    /// <param name="payload">The parsed payload, or null.</param>
    public static NowPlayingInfo? MapPayload(MediaRemoteAdapterPayload? payload)
    {
        if (payload is null)
        {
            return null;
        }

        string? title = NullIfEmpty(payload.Title);
        string? artist = NullIfEmpty(payload.Artist);
        string? album = NullIfEmpty(payload.Album);

        if (title is null && artist is null && album is null)
        {
            return null;
        }

        return new NowPlayingInfo(title, artist, album);
    }

    private static string? NullIfEmpty(string? value)
    {
        if (value is null)
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
