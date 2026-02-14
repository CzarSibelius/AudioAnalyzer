namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides list of available audio devices and creates IAudioInput for a selection.
/// </summary>
public interface IAudioDeviceInfo
{
    /// <summary>
    /// Display name and an opaque id for creating capture.
    /// </summary>
    IReadOnlyList<AudioDeviceEntry> GetDevices();

    /// <summary>
    /// Create an IAudioInput for the given device id (from GetDevices). Id can be null for default loopback.
    /// </summary>
    IAudioInput CreateCapture(string? deviceId);
}
