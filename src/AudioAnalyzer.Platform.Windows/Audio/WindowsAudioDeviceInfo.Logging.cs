using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.Windows.Audio;

public sealed partial class WindowsAudioDeviceInfo
{
    [LoggerMessage(EventId = 7660, Level = LogLevel.Warning, Message = "WASAPI device enumeration failed; Demo modes remain available.")]
    private partial void LogWasapiEnumerationFailed(Exception ex);
}
