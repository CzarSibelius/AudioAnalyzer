using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

public sealed partial class MacOsCoreAudioAudioInput
{
    [LoggerMessage(EventId = 7730, Level = LogLevel.Error, Message = "AudioQueueStart failed: OSStatus={Status}")]
    private partial void LogAudioQueueStartFailed(int status);

    [LoggerMessage(EventId = 7731, Level = LogLevel.Warning, Message = "AudioQueueSetProperty(CurrentDevice) failed for UID {Uid}: OSStatus={Status}; using default input routing.")]
    private partial void LogCurrentDevicePropertyFailed(string uid, int status);

    [LoggerMessage(EventId = 7737, Level = LogLevel.Warning, Message = "No audio captured within {Seconds}s for UID {Uid}. If this is a microphone/input device, grant Microphone access to the host in System Settings → Privacy & Security → Microphone (signed-bundle TCC, ADR-0088); persistent silence with no callbacks usually means access was denied or the device delivers no data.")]
    private partial void LogCaptureSilenceHint(double seconds, string uid);

    [LoggerMessage(EventId = 7736, Level = LogLevel.Warning, Message = "AudioQueueSetProperty(ChannelLayout) failed: OSStatus={Status}")]
    private partial void LogChannelLayoutFailed(int status);

    [LoggerMessage(EventId = 7732, Level = LogLevel.Error, Message = "AudioQueueNewInput failed: OSStatus={Status}")]
    private partial void LogAudioQueueNewInputFailed(int status);

    [LoggerMessage(EventId = 7733, Level = LogLevel.Error, Message = "AudioQueueAllocateBuffer failed: OSStatus={Status}")]
    private partial void LogAllocateBufferFailed(int status);

    [LoggerMessage(EventId = 7734, Level = LogLevel.Error, Message = "AudioQueueEnqueueBuffer failed: OSStatus={Status}")]
    private partial void LogEnqueueBufferFailed(int status);

    [LoggerMessage(EventId = 7735, Level = LogLevel.Information, Message = "Core Audio capture opened for UID {Uid}: rate={Rate}, channels={Channels}, bits={Bits}, flags={Flags:X8}")]
    private partial void LogCaptureOpened(string uid, double rate, uint channels, uint bits, uint flags);
}
