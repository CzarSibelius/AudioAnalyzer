using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio;

public sealed partial class MacOsScreenCaptureKitSystemAudioInput
{
    [LoggerMessage(EventId = 7750, Level = LogLevel.Warning, Message = "ScreenCaptureKit: could not prepare capture.")]
    private partial void LogSckPrepareFailed(Exception ex);

    [LoggerMessage(EventId = 7751, Level = LogLevel.Warning, Message = "ScreenCaptureKit: start capture failed: {Message}")]
    private partial void LogSckStartFailed(string message);

    [LoggerMessage(EventId = 7752, Level = LogLevel.Warning, Message = "ScreenCaptureKit: no displays in shareable content.")]
    private partial void LogSckNoDisplays();

    [LoggerMessage(EventId = 7753, Level = LogLevel.Warning, Message = "ScreenCaptureKit: AddStreamOutput failed: {Message}")]
    private partial void LogSckAddOutputFailed(string message);

    [LoggerMessage(EventId = 7754, Level = LogLevel.Warning, Message = "ScreenCaptureKit: capture preparation timed out after {TimeoutSeconds}s.")]
    private partial void LogSckPrepareTimeout(double timeoutSeconds);
}
