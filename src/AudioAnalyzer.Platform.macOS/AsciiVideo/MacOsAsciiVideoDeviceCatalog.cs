using System.Globalization;
using System.Runtime.InteropServices;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.AsciiVideo;

/// <summary>Enumerates AVFoundation video capture devices for settings display names.</summary>
public sealed class MacOsAsciiVideoDeviceCatalog : IAsciiVideoDeviceCatalog
{
    private const int NameBufferBytes = 256;

    /// <summary>How long to reuse enumeration results to avoid querying AVFoundation every frame.</summary>
    private static readonly TimeSpan s_cacheTtl = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();
    private IReadOnlyList<AsciiVideoDeviceEntry>? _cache;
    private DateTime _cacheUtc;

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
        if (!MacOsCameraCaptureAvailability.IsCaptureReady)
        {
            return Array.Empty<AsciiVideoDeviceEntry>();
        }

        int count;
        try
        {
            count = MacOsVideoCaptureShimNative.VideoCaptureDeviceCount();
        }
        catch (DllNotFoundException)
        {
            return Array.Empty<AsciiVideoDeviceEntry>();
        }

        if (count <= 0)
        {
            return Array.Empty<AsciiVideoDeviceEntry>();
        }

        var list = new List<AsciiVideoDeviceEntry>(count);
        IntPtr nameBuffer = Marshal.AllocHGlobal(NameBufferBytes);
        try
        {
            for (int i = 0; i < count; i++)
            {
                string name = string.Format(CultureInfo.InvariantCulture, "Camera {0}", i);
                if (MacOsVideoCaptureShimNative.VideoCaptureDeviceName(i, nameBuffer, (UIntPtr)NameBufferBytes) == 0)
                {
                    string? resolved = Marshal.PtrToStringUTF8(nameBuffer);
                    if (!string.IsNullOrWhiteSpace(resolved))
                    {
                        name = resolved.Trim();
                    }
                }

                list.Add(new AsciiVideoDeviceEntry(i, name));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(nameBuffer);
        }

        return list;
    }
}
