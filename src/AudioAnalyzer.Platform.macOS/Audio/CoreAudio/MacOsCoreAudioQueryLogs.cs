using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudio;

internal static class MacOsCoreAudioQueryLogs
{
    internal static readonly Action<ILogger, Exception?> UnexpectedEnumerationError = LoggerMessage.Define(
        LogLevel.Error,
        new EventId(7721, "CoreAudioEnumerationUnexpected"),
        "Unexpected error enumerating Core Audio devices.");

    internal static readonly Action<ILogger, string, int, Exception?> EnumerationStepFailed = LoggerMessage.Define<string, int>(
        LogLevel.Warning,
        new EventId(7722, "CoreAudioEnumerationStep"),
        "Core Audio device enumeration failed at {Step}: OSStatus={Status}");

    internal static readonly Action<ILogger, uint, uint, uint, int, Exception?> PropertyReadFailed = LoggerMessage.Define<uint, uint, uint, int>(
        LogLevel.Trace,
        new EventId(7723, "CoreAudioPropertyRead"),
        "Core Audio property read failed (selector={Selector:X8}, scope={Scope:X8}, device={Device}): OSStatus={Status}");

    internal static readonly Action<ILogger, uint, int, Exception?> InputFormatUnavailable = LoggerMessage.Define<uint, int>(
        LogLevel.Trace,
        new EventId(7724, "CoreAudioInputFormat"),
        "Core Audio input stream format unavailable for device {Device}: OSStatus={Status}");

    internal static readonly Action<ILogger, int, Exception?> DefaultInputReadFailed = LoggerMessage.Define<int>(
        LogLevel.Trace,
        new EventId(7725, "CoreAudioDefaultInput"),
        "Could not read default input device: OSStatus={Status}");
}
