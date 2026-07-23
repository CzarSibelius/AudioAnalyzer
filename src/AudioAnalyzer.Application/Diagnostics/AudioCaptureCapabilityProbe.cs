using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Diagnostics;

/// <summary>
/// Cross-platform core audio-capture probe: reports <see cref="FeatureCapabilityIds.AudioCapture"/>
/// available when host device enumeration succeeds and yields at least one device.
/// </summary>
public sealed class AudioCaptureCapabilityProbe : IFeatureCapabilityProbe
{
    private readonly IAudioDeviceInfo _deviceInfo;

    /// <summary>Initializes a new instance of the <see cref="AudioCaptureCapabilityProbe"/> class.</summary>
    /// <param name="deviceInfo">Host audio device enumeration.</param>
    public AudioCaptureCapabilityProbe(IAudioDeviceInfo deviceInfo)
    {
        _deviceInfo = deviceInfo ?? throw new ArgumentNullException(nameof(deviceInfo));
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> Probe()
    {
        FeatureAvailability availability;
        string detail;
        try
        {
            var devices = _deviceInfo.GetDevices();
            int count = devices?.Count ?? 0;
            if (count > 0)
            {
                availability = FeatureAvailability.Available;
                detail = devices![0].Name;
            }
            else
            {
                availability = FeatureAvailability.Unavailable;
                detail = "no capture devices found";
            }
        }
        catch (Exception ex)
        {
            availability = FeatureAvailability.Unavailable;
            detail = ex.Message;
        }

        return
        [
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.AudioCapture,
                "Audio capture",
                availability,
                detail,
                FeatureCapabilityCategory.Audio)
        ];
    }
}
