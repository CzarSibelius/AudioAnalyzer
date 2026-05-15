using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

public sealed partial class MacOsCoreAudioEnumerator
{
    [LoggerMessage(EventId = 7742, Level = LogLevel.Information, Message = "Core Audio: enumerating physical input devices")]
    private partial void LogEnumeratePhysicalInputsBegin();

    [LoggerMessage(EventId = 7743, Level = LogLevel.Information, Message = "Core Audio: enumeration finished ({Count} inputs)")]
    private partial void LogEnumeratePhysicalInputsEnd(int count);

    [LoggerMessage(EventId = 7740, Level = LogLevel.Warning, Message = "No Core Audio input device matches UID {Uid}")]
    private partial void LogUnknownUid(string uid);

    [LoggerMessage(EventId = 7741, Level = LogLevel.Error, Message = "Failed to construct Core Audio capture for UID {Uid}")]
    private partial void LogConstructFailed(Exception ex, string uid);
}
