namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Supplies BPM, beat count, and beat flash for <see cref="AnalysisEngine"/> and <see cref="AnalysisSnapshot"/>.
/// Audio FFT/waveform/volume are computed separately from captured buffers.
/// </summary>
public interface IBeatTimingSource
{
    /// <summary>Current tempo in BPM (0 when unknown / unavailable).</summary>
    double CurrentBpm { get; }

    /// <summary>Monotonic beat counter for layers and show timing.</summary>
    int BeatCount { get; }

    /// <summary>True briefly on each beat boundary for UI flash.</summary>
    bool BeatFlashActive { get; }

    /// <summary>
    /// Beat detection sensitivity; meaningful for energy-based audio detection only.
    /// Other sources may ignore writes or clamp.
    /// </summary>
    double BeatSensitivity { get; set; }

    /// <summary>Called once per audio buffer with average energy (mono RMS²); audio-derived sources use it.</summary>
    void OnAudioFrame(double avgEnergy);

    /// <summary>
    /// Called on the same ~50 ms cadence as header refresh: flash decay, demo/link beat advancement.
    /// </summary>
    void OnVisualTick();
}
