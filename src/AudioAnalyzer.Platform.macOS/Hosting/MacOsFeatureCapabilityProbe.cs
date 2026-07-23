using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.AsciiVideo;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;
using AudioAnalyzer.Platform.macOS.NowPlaying;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>
/// macOS shim/integration capability probe: system-audio tap, ASCII video (webcam), now playing
/// (mediaremote-adapter), and screen dump (unavailable on macOS). Reuses the existing availability
/// types; the macOS permission rows are contributed separately by
/// <see cref="Permissions.MacOsPermissionCapabilityProbe"/>.
/// </summary>
internal sealed class MacOsFeatureCapabilityProbe : IFeatureCapabilityProbe
{
    private readonly MacOsMediaRemoteAdapterAvailability _nowPlayingAvailability;

    /// <summary>Initializes a new instance of the <see cref="MacOsFeatureCapabilityProbe"/> class.</summary>
    /// <param name="contentLocator">Bundle content locator used to resolve now-playing artifacts.</param>
    public MacOsFeatureCapabilityProbe(IHostContentLocator contentLocator)
    {
        ArgumentNullException.ThrowIfNull(contentLocator);
        _nowPlayingAvailability = new MacOsMediaRemoteAdapterAvailability(contentLocator);
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> Probe()
    {
        bool tapReady = MacOsCoreAudioTapAvailability.IsCaptureReady;
        bool cameraReady = MacOsCameraCaptureAvailability.IsCaptureReady;
        bool nowPlayingReady = _nowPlayingAvailability.IsAvailable;

        return
        [
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.SystemAudioTap,
                "System audio tap",
                tapReady ? FeatureAvailability.Available : FeatureAvailability.Unavailable,
                tapReady ? "" : "no native audio tap shim (or macOS < 14.2)",
                FeatureCapabilityCategory.Audio),
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.AsciiVideo,
                "ASCII video (webcam)",
                cameraReady ? FeatureAvailability.Available : FeatureAvailability.Unavailable,
                cameraReady ? "" : "no native video capture shim",
                FeatureCapabilityCategory.Visual),
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.NowPlaying,
                "Now playing",
                nowPlayingReady ? FeatureAvailability.Available : FeatureAvailability.Unavailable,
                nowPlayingReady ? "" : "mediaremote-adapter not present",
                FeatureCapabilityCategory.Integration),
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.ScreenDump,
                "Screen dump",
                FeatureAvailability.Unavailable,
                "not supported on macOS",
                FeatureCapabilityCategory.Integration)
        ];
    }
}
