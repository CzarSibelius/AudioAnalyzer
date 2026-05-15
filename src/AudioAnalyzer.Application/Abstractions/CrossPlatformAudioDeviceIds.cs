namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Stable synthetic device ids that may appear in the device list or persisted settings across hosts.
/// </summary>
public static class CrossPlatformAudioDeviceIds
{
    /// <summary>
    /// macOS list entry: prefer capturing a user-installed virtual desktop mixer when present (see ADR-0085).
    /// </summary>
    public const string MacOsDesktopVirtualRouting = "macos-desktop-virtual-routing";

    /// <summary>
    /// macOS list entry: system/desktop output via Screen Recording + ScreenCaptureKit (see ADR-0086, PBI-016).
    /// </summary>
    public const string MacOsScreenCaptureKitSystemAudio = "macos-sck-system-audio";
}
