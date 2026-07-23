namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Stable identifiers for the well-known feature capabilities (shared by probes, log, tests).</summary>
public static class FeatureCapabilityIds
{
    /// <summary>Core audio capture (WASAPI loopback on Windows, Core Audio mic/input on macOS).</summary>
    public const string AudioCapture = "audio-capture";

    /// <summary>Ableton Link integration (native <c>link_shim</c>).</summary>
    public const string AbletonLink = "ableton-link";

    /// <summary>macOS system-audio process tap (<c>libaudio_tap_shim.dylib</c>).</summary>
    public const string SystemAudioTap = "system-audio-tap";

    /// <summary>ASCII video / webcam layer.</summary>
    public const string AsciiVideo = "ascii-video";

    /// <summary>Now playing integration (GSMTC on Windows, mediaremote-adapter on macOS).</summary>
    public const string NowPlaying = "now-playing";

    /// <summary>Screen dump (ASCII screenshot).</summary>
    public const string ScreenDump = "screen-dump";

    /// <summary>macOS System Audio Recording permission (TCC).</summary>
    public const string PermissionSystemAudio = "permission-system-audio";

    /// <summary>macOS Microphone permission (TCC).</summary>
    public const string PermissionMicrophone = "permission-microphone";

    /// <summary>macOS Camera permission (TCC).</summary>
    public const string PermissionCamera = "permission-camera";
}
