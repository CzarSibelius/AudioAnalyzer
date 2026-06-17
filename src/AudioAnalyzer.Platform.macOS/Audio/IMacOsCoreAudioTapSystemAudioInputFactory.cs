using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Constructs <see cref="IAudioInput"/> for Core Audio process tap system audio (test seam).</summary>
public interface IMacOsCoreAudioTapSystemAudioInputFactory
{
    /// <summary>Creates a new capture instance (not started until <see cref="IAudioInput.Start"/>).</summary>
    IAudioInput Create();
}
