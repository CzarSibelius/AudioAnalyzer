using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

internal sealed partial class DeviceCaptureController
{
    [LoggerMessage(EventId = 7780, Level = LogLevel.Information, Message = "Device UI: releasing capture for picker (thread {ThreadId})")]
    private partial void LogReleaseCaptureForSelectionBegin(int threadId);

    [LoggerMessage(EventId = 7781, Level = LogLevel.Information, Message = "Device UI: capture released for picker")]
    private partial void LogReleaseCaptureForSelectionEnd();
}
