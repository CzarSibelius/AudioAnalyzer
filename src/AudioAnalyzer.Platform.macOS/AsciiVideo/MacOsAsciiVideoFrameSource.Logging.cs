using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.AsciiVideo;

public sealed partial class MacOsAsciiVideoFrameSource
{
    [LoggerMessage(EventId = 7660, Level = LogLevel.Error, Message = "AsciiVideo command loop failed.")]
    private partial void LogAsciiVideoCommandLoopFailed(Exception ex);

    [LoggerMessage(EventId = 7661, Level = LogLevel.Warning, Message = "AsciiVideo worker shutdown failed.")]
    private partial void LogAsciiVideoWorkerShutdownFailed(Exception ex);

    [LoggerMessage(EventId = 7662, Level = LogLevel.Warning, Message = "AsciiVideo frame processing failed.")]
    private partial void LogAsciiVideoFrameProcessingFailed(Exception ex);

    [LoggerMessage(EventId = 7663, Level = LogLevel.Warning, Message = "AsciiVideo: start session failed: {Message}")]
    private partial void LogAsciiVideoStartSessionFailed(string message);

    [LoggerMessage(EventId = 7664, Level = LogLevel.Warning, Message = "AsciiVideo: start session threw.")]
    private partial void LogAsciiVideoStartSessionThrew(Exception ex);

    [LoggerMessage(EventId = 7665, Level = LogLevel.Warning, Message = "AsciiVideo: stop session failed.")]
    private partial void LogAsciiVideoStopFailed(Exception ex);

    [LoggerMessage(EventId = 7666, Level = LogLevel.Warning, Message = "AsciiVideo: libvideo_capture_shim.dylib not found; build native/video-capture-shim. Camera disabled.")]
    private partial void LogAsciiVideoShimMissing();
}
