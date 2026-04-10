using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure.AsciiVideo;

/// <summary>Empty device list when capture enumeration is unavailable.</summary>
public sealed class NullAsciiVideoDeviceCatalog : IAsciiVideoDeviceCatalog
{
    /// <inheritdoc />
    public IReadOnlyList<AsciiVideoDeviceEntry> GetDevices() => Array.Empty<AsciiVideoDeviceEntry>();
}
