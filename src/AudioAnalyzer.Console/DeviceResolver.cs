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
            var first = devices[0];
            return (first.Id, first.Name);
        }

        if (settings.InputMode == "device" && !string.IsNullOrEmpty(settings.DeviceName))
        {
            var captureId = CapturePrefix + settings.DeviceName;
            var loopbackId = LoopbackPrefix + settings.DeviceName;
            foreach (var d in devices)
            {
                if (d.Id == captureId || d.Id == loopbackId)
                {
                    return (d.Id, d.Name);
                }
            }
        }

        return (null, "");
    }
}
