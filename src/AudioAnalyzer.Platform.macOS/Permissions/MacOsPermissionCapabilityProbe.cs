using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Platform.macOS.AsciiVideo;
using AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;

namespace AudioAnalyzer.Platform.macOS.Permissions;

/// <summary>
/// Reports macOS TCC permission grants (System Audio Recording, Microphone, Camera) as
/// <see cref="FeatureCapabilityCategory.Permission"/> capabilities using <b>non-prompting</b>
/// status/preflight queries (never raises a consent prompt; prompting stays at capture start per
/// ADR-0091). When a shim is missing the row is reported unavailable with a hint.
/// </summary>
internal sealed class MacOsPermissionCapabilityProbe : IFeatureCapabilityProbe
{
    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> Probe()
    {
        var (sysAvailability, sysDetail) = QuerySystemAudio();
        var (micAvailability, micDetail) = QueryAv(mediaIsAudio: 1, "Microphone");
        var (camAvailability, camDetail) = QueryAv(mediaIsAudio: 0, "Camera");

        return
        [
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.PermissionSystemAudio,
                "System Audio Recording",
                sysAvailability,
                sysDetail,
                FeatureCapabilityCategory.Permission),
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.PermissionMicrophone,
                "Microphone",
                micAvailability,
                micDetail,
                FeatureCapabilityCategory.Permission),
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.PermissionCamera,
                "Camera",
                camAvailability,
                camDetail,
                FeatureCapabilityCategory.Permission)
        ];
    }

    private static (FeatureAvailability Availability, string Detail) QuerySystemAudio()
    {
        int status;
        try
        {
            status = MacOsAudioTapShimNative.AudioTapPermissionStatus();
        }
        catch (DllNotFoundException)
        {
            return (FeatureAvailability.Unavailable, "audio tap shim not loaded");
        }
        catch (EntryPointNotFoundException)
        {
            return (FeatureAvailability.Unavailable, "audio tap shim outdated");
        }

        return MacOsPermissionAvailabilityMapping.FromSystemAudioPreflight(status);
    }

    private static (FeatureAvailability Availability, string Detail) QueryAv(int mediaIsAudio, string label)
    {
        int status;
        try
        {
            status = MacOsVideoCaptureShimNative.VideoCaptureAuthorizationStatus(mediaIsAudio);
        }
        catch (DllNotFoundException)
        {
            return (FeatureAvailability.Unavailable, "video capture shim not loaded");
        }
        catch (EntryPointNotFoundException)
        {
            return (FeatureAvailability.Unavailable, "video capture shim outdated");
        }

        return MacOsPermissionAvailabilityMapping.FromAvAuthorizationStatus(status, label);
    }
}
