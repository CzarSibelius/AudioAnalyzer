using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Audio;

/// <summary>Windows loopback fallback: the first listed device (WASAPI loopback is the default capture path).</summary>
public sealed class WindowsDefaultDeviceFallbackPolicy : IDefaultDeviceFallbackPolicy
{
    /// <inheritdoc />
    public (string? deviceId, string name) ResolveLoopbackFallback(IReadOnlyList<AudioDeviceEntry> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        if (devices.Count == 0)
        {
            return (null, "");
        }

        return (devices[0].Id, devices[0].Name);
    }
}
