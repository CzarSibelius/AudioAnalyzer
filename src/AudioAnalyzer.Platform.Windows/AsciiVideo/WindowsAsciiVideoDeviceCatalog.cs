using System.Runtime.Versioning;
using AudioAnalyzer.Application.Abstractions;
using Windows.Media.Capture.Frames;

namespace AudioAnalyzer.Platform.Windows.AsciiVideo;

/// <summary>Enumerates <see cref="MediaFrameSourceGroup"/> entries for settings display names.</summary>
[SupportedOSPlatform("windows10.0.19041.0")]
public sealed class WindowsAsciiVideoDeviceCatalog : IAsciiVideoDeviceCatalog
{
    private readonly object _lock = new();
    private IReadOnlyList<AsciiVideoDeviceEntry>? _cache;
    private DateTime _cacheUtc;

    /// <summary>How long to reuse enumeration results to avoid blocking the UI thread every frame.</summary>
    private static readonly TimeSpan s_cacheTtl = TimeSpan.FromSeconds(30);

    /// <inheritdoc />
    public IReadOnlyList<AsciiVideoDeviceEntry> GetDevices()
    {
        lock (_lock)
        {
            if (_cache != null && DateTime.UtcNow - _cacheUtc < s_cacheTtl)
            {
                return _cache;
            }

            _cache = RefreshDevices();
            _cacheUtc = DateTime.UtcNow;
            return _cache;
        }
    }

    private static IReadOnlyList<AsciiVideoDeviceEntry> RefreshDevices()
    {
        try
        {
            var task = MediaFrameSourceGroup.FindAllAsync().AsTask();
            if (!task.Wait(TimeSpan.FromSeconds(3)))
            {
                return Array.Empty<AsciiVideoDeviceEntry>();
            }

            if (task.IsFaulted)
            {
                return Array.Empty<AsciiVideoDeviceEntry>();
            }

            IReadOnlyList<MediaFrameSourceGroup> groups = task.Result;
            if (groups.Count == 0)
            {
                return Array.Empty<AsciiVideoDeviceEntry>();
            }

            var list = new List<AsciiVideoDeviceEntry>(groups.Count);
            for (int i = 0; i < groups.Count; i++)
            {
                string name = string.IsNullOrWhiteSpace(groups[i].DisplayName)
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "Camera {0}", i)
                    : groups[i].DisplayName.Trim();
                list.Add(new AsciiVideoDeviceEntry(i, name));
            }

            return list;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AsciiVideo device list: {ex.Message}");
            return Array.Empty<AsciiVideoDeviceEntry>();
        }
    }
}
