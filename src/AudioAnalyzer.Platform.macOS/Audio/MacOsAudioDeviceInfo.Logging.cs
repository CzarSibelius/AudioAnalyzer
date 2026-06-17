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

    [LoggerMessage(EventId = 7725, Level = LogLevel.Warning, Message = "Core Audio tap device is listed but libaudio_tap_shim.dylib is missing next to the app. Build native/audio-tap-shim and rebuild the macOS host (see native/README.md).")]
    private partial void LogCoreAudioTapShimNotLoaded();
}
