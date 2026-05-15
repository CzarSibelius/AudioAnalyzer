using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

public sealed partial class MacOsAudioDeviceInfo
{
    [LoggerMessage(EventId = 7721, Level = LogLevel.Information, Message = "macOS audio device list: building entries")]
    private partial void LogGetDevicesBegin();

    [LoggerMessage(EventId = 7722, Level = LogLevel.Information, Message = "macOS audio device list: complete ({Count} entries)")]
    private partial void LogGetDevicesEnd(int count);

    [LoggerMessage(EventId = 7720, Level = LogLevel.Warning, Message = "Unknown or unavailable audio device id {DeviceId}; falling back to Demo synthesis (120 BPM).")]
    private partial void LogUnknownDevice(string deviceId);

    [LoggerMessage(EventId = 7723, Level = LogLevel.Information, Message = "macOS desktop routing: capturing virtual mixer \"{DeviceName}\" for system output visualization.")]
    private partial void LogDesktopVirtualRoutingPicked(string deviceName);

    [LoggerMessage(EventId = 7724, Level = LogLevel.Warning, Message = "macOS desktop routing: no recognized virtual mixer (e.g. BlackHole) found; falling back to Demo synthesis (120 BPM). Install a virtual device and route Mac playback to it (see docs/getting-started.md).")]
    private partial void LogDesktopVirtualRoutingNoSinkFound();
}
