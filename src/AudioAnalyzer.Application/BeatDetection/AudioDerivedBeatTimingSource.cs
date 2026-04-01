using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>
/// Beat timing from energy-based <see cref="BeatDetector"/> on the audio stream.
/// </summary>
public sealed class AudioDerivedBeatTimingSource : IBeatTimingSource
{
    private readonly IBeatDetector _detector;

    /// <summary>Creates a wrapper around the shared beat detector instance.</summary>
    public AudioDerivedBeatTimingSource(IBeatDetector detector)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    /// <inheritdoc />
    public double CurrentBpm => _detector.CurrentBpm;

    /// <inheritdoc />
    public int BeatCount => _detector.BeatCount;

    /// <inheritdoc />
    public bool BeatFlashActive => _detector.BeatFlashActive;

    /// <inheritdoc />
    public double BeatSensitivity
    {
        get => _detector.BeatSensitivity;
        set => _detector.BeatSensitivity = value;
    }

    /// <inheritdoc />
    public void OnAudioFrame(double avgEnergy) => _detector.ProcessFrame(avgEnergy);

    /// <inheritdoc />
    public void OnVisualTick() => _detector.DecayFlashFrame();
}
