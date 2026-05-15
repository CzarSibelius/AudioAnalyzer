using System.Diagnostics.CodeAnalysis;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Stable device id encoding for Core Audio inputs on macOS (see PBI-013 / ADR-0084).</summary>
public static class MacOsAudioDeviceIds
{
    /// <summary>Prefix for microphone / line-in devices enumerated via Core Audio. Not used for Demo synthesis ids.</summary>
    public const string InputPrefix = "macos-input:";

    /// <summary>Builds a persisted device id from the Core Audio device UID string.</summary>
    public static string EncodeInputUid(string uid) => InputPrefix + Uri.EscapeDataString(uid);

    /// <summary>Parses <see cref="InputPrefix"/> ids back to the Core Audio UID.</summary>
    public static bool TryDecodeInputUid(string deviceId, [NotNullWhen(true)] out string? uid)
    {
        uid = null;
        if (string.IsNullOrEmpty(deviceId) || !deviceId.StartsWith(InputPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            uid = Uri.UnescapeDataString(deviceId.AsSpan(InputPrefix.Length).ToString());
            return !string.IsNullOrEmpty(uid);
        }
        catch (UriFormatException)
        {
            return false;
        }
    }
}
