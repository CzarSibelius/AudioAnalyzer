namespace AudioAnalyzer.Domain;

/// <summary>
/// Helpers for synthetic demo device ids (<c>demo:120</c>) used by <see cref="BpmSource.DemoDevice"/>.
/// </summary>
public static class DemoAudioDevice
{
    /// <summary>Prefix for demo device ids in the audio device list.</summary>
    public const string Prefix = "demo:";

    /// <summary>
    /// Parses BPM from a device id when it starts with <see cref="Prefix"/>; otherwise returns false.
    /// </summary>
    public static bool TryGetBpm(string? deviceId, out int bpm)
    {
        bpm = 120;
        if (string.IsNullOrEmpty(deviceId) ||
            !deviceId.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        ReadOnlySpan<char> span = deviceId.AsSpan(Prefix.Length);
        return int.TryParse(span, out bpm) && bpm > 0;
    }
}
