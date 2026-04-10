using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.Windows.AsciiVideo;

public sealed partial class WindowsAsciiVideoFrameSource
{
    [LoggerMessage(EventId = 7650, Level = LogLevel.Error, Message = "AsciiVideo command loop failed.")]
    private partial void LogAsciiVideoCommandLoopFailed(Exception ex);

    [LoggerMessage(EventId = 7651, Level = LogLevel.Warning, Message = "AsciiVideo worker shutdown failed.")]
    private partial void LogAsciiVideoWorkerShutdownFailed(Exception ex);

    [LoggerMessage(EventId = 7652, Level = LogLevel.Warning, Message = "AsciiVideo frame processing failed.")]
    private partial void LogAsciiVideoFrameProcessingFailed(Exception ex);

    [LoggerMessage(EventId = 7653, Level = LogLevel.Warning, Message = "AsciiVideo: start session failed.")]
    private partial void LogAsciiVideoStartSessionFailed(Exception ex);

    [LoggerMessage(EventId = 7654, Level = LogLevel.Debug, Message = "AsciiVideo: format cap failed.")]
    private partial void LogAsciiVideoFormatCapFailed(Exception ex);

    [LoggerMessage(EventId = 7655, Level = LogLevel.Warning, Message = "AsciiVideo: reader stop failed.")]
    private partial void LogAsciiVideoReaderStopFailed(Exception ex);

    [LoggerMessage(EventId = 7656, Level = LogLevel.Warning, Message = "AsciiVideo: no camera groups found.")]
    private partial void LogAsciiVideoNoCameraGroups();

    [LoggerMessage(EventId = 7657, Level = LogLevel.Warning, Message = "AsciiVideo: no usable frame source.")]
    private partial void LogAsciiVideoNoFrameSource();
}
