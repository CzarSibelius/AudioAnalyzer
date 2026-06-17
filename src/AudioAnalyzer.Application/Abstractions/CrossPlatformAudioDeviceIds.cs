namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Stable synthetic device ids that may appear in the device list or persisted settings across hosts.
/// </summary>
public static class CrossPlatformAudioDeviceIds
{
    /// <summary>
    /// macOS list entry: system/desktop output via Core Audio process taps (macOS 14.2+, System Audio Recording consent).
    /// </summary>
    public const string MacOsCoreAudioTapSystemAudio = "macos-coreaudio-tap-system-audio";
}
