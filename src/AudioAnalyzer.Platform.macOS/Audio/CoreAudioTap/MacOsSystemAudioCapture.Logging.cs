using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Platform.macOS.Audio.CoreAudioTap;

public sealed partial class MacOsSystemAudioCapture
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Core Audio tap start failed: {Message}")]
    private partial void LogStartFailed(string message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Core Audio tap capture started ({SampleRate} Hz, {Channels} ch).")]
    private partial void LogCaptureStarted(double sampleRate, int channels);

    [LoggerMessage(Level = LogLevel.Information, Message = "Core Audio tap PCM activity: chunks={Chunks}, bytes={Bytes}, peak={Peak}, isFloat={IsFloat}, bits={Bits}, ch={Channels}.")]
    private partial void LogPcmActivity(long chunks, uint bytes, float peak, bool isFloat, int bits, int channels);
}
