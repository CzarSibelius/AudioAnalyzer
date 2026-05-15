using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Constructs <see cref="IAudioInput"/> for ScreenCaptureKit system audio (test seam).</summary>
public interface IMacOsScreenCaptureKitSystemAudioInputFactory
{
    /// <summary>Creates a new capture instance (not started until <see cref="IAudioInput.Start"/>).</summary>
    IAudioInput Create();
}
