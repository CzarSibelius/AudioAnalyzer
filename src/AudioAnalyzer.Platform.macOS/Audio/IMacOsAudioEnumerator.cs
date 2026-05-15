using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Abstracts Core Audio device discovery and capture construction for tests (PBI-013).</summary>
public interface IMacOsAudioEnumerator
{
    /// <summary>Returns physical input devices available on this host (microphones, aggregate inputs), excluding Demo synthesis.</summary>
    IReadOnlyList<MacOsPhysicalAudioDevice> GetPhysicalInputs();

    /// <summary>Creates capture for a <see cref="MacOsAudioDeviceIds.InputPrefix"/> device id, or returns false.</summary>
    bool TryCreateCapture(string deviceId, out IAudioInput? input);
}
