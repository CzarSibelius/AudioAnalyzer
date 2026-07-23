using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Hosting;

/// <summary>
/// Windows shim/integration capability probe: ASCII video (WinRT MediaCapture), now playing (GSMTC),
/// and screen dump (kernel32 console APIs). These OS features are always present on the supported
/// Windows host, so they report available. The macOS-only permission rows are not applicable on
/// Windows and are omitted (hidden in the hub).
/// </summary>
public sealed class WindowsFeatureCapabilityProbe : IFeatureCapabilityProbe
{
    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> Probe() =>
    [
        new FeatureCapabilityStatus(
            FeatureCapabilityIds.AsciiVideo,
            "ASCII video (webcam)",
            FeatureAvailability.Available,
            "",
            FeatureCapabilityCategory.Visual),
        new FeatureCapabilityStatus(
            FeatureCapabilityIds.NowPlaying,
            "Now playing",
            FeatureAvailability.Available,
            "",
            FeatureCapabilityCategory.Integration),
        new FeatureCapabilityStatus(
            FeatureCapabilityIds.ScreenDump,
            "Screen dump",
            FeatureAvailability.Available,
            "",
            FeatureCapabilityCategory.Integration)
    ];
}
