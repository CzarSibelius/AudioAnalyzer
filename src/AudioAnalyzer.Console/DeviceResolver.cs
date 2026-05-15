using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Resolves the initial audio device from persisted settings or device list.</summary>
internal static class DeviceResolver
{
    private const string CapturePrefix = "capture:";
    private const string LoopbackPrefix = "loopback:";

    /// <summary>Attempts to resolve a device id and name from the given devices and persisted app settings.</summary>
    public static (string? deviceId, string name) TryResolveFromSettings(
        IReadOnlyList<AudioDeviceEntry> devices,
        AppSettings settings)
    {
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
            // Prefer Demo so startup does not auto-open ScreenCaptureKit / virtual-routing capture (ADR-0084).
            if (OperatingSystem.IsMacOS())
            {
                AudioDeviceEntry? demo = devices.FirstOrDefault(d =>
                    d.Id != null && d.Id.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal));
                if (demo != null)
                {
                    return (demo.Id, demo.Name);
                }
            }

            AudioDeviceEntry? macDesktop = devices.FirstOrDefault(d =>
                string.Equals(d.Id, CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting, StringComparison.Ordinal));
            if (macDesktop != null)
            {
                return (macDesktop.Id, macDesktop.Name);
            }

            AudioDeviceEntry? macSck = devices.FirstOrDefault(d =>
                string.Equals(d.Id, CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio, StringComparison.Ordinal));
            if (macSck != null)
            {
                return (macSck.Id, macSck.Name);
            }

            return (devices[0].Id, devices[0].Name);
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
