using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

public sealed partial class MacOsCoreAudioTapAudioInput
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Core Audio tap capture unavailable (missing dylib, OS version, or load failure).")]
    private partial void LogTapUnavailable();

    [LoggerMessage(Level = LogLevel.Error, Message = "Core Audio tap capture failed to start.")]
    private partial void LogTapStartFailed(Exception ex);
}
