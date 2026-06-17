using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Resolves the initial audio device from persisted settings or device list.</summary>
internal static class DeviceResolver
{
    private const string CapturePrefix = "capture:";
    private const string LoopbackPrefix = "loopback:";

    /// <summary>Attempts to resolve a device id and name from the given devices and persisted app settings.</summary>
    /// <param name="devices">Available devices.</param>
    /// <param name="settings">Persisted application settings.</param>
    /// <param name="fallbackPolicy">Platform policy for the loopback fallback when no system-audio entry exists.</param>
    public static (string? deviceId, string name) TryResolveFromSettings(
        IReadOnlyList<AudioDeviceEntry> devices,
        AppSettings settings,
        IDefaultDeviceFallbackPolicy fallbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(fallbackPolicy);

        if (devices.Count == 0)
        {
            return (null, "");
        }

        if (settings.InputMode == "loopback" && string.IsNullOrEmpty(settings.DeviceName))
        {
            AudioDeviceEntry? systemAudio = devices.FirstOrDefault(d => d.Id == null);
            if (systemAudio != null)
            {
                return (systemAudio.Id, systemAudio.Name);
            }

            // macOS has no WASAPI loopback; fresh settings use InputMode=loopback (Windows default).
            // Prefer the Core Audio system-audio tap as the "what you hear" default (ADR-0089);
            // the platform fallback policy then decides between Demo and the first listed device.
            AudioDeviceEntry? macTap = devices.FirstOrDefault(d =>
                string.Equals(d.Id, CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio, StringComparison.Ordinal));
            if (macTap != null)
            {
                return (macTap.Id, macTap.Name);
            }

            return fallbackPolicy.ResolveLoopbackFallback(devices);
        }

        if (settings.InputMode == "device" && !string.IsNullOrEmpty(settings.DeviceName))
        {
            var captureId = CapturePrefix + settings.DeviceName;
            var loopbackId = LoopbackPrefix + settings.DeviceName;
            foreach (var d in devices)
            {
                if (d.Id == captureId || d.Id == loopbackId || d.Id == settings.DeviceName)
                {
                    return (d.Id, d.Name);
                }
            }
        }

        return (null, "");
    }
}
