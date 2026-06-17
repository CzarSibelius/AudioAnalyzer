using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>
/// macOS loopback fallback: prefer Demo when no system-audio tap is present (fresh settings use
/// InputMode=loopback, the Windows default), then the first listed device. See ADR-0089.
/// </summary>
public sealed class MacOsDefaultDeviceFallbackPolicy : IDefaultDeviceFallbackPolicy
{
    /// <inheritdoc />
    public (string? deviceId, string name) ResolveLoopbackFallback(IReadOnlyList<AudioDeviceEntry> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        if (devices.Count == 0)
        {
            return (null, "");
        }

        AudioDeviceEntry? demo = devices.FirstOrDefault(d =>
            d.Id != null && d.Id.StartsWith(DemoAudioDevice.Prefix, StringComparison.Ordinal));
        if (demo != null)
        {
            return (demo.Id, demo.Name);
        }

        return (devices[0].Id, devices[0].Name);
    }
}
