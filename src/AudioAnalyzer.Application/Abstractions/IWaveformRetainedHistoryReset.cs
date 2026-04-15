namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Clears mono/stereo waveform history rings, decimated overview, beat-mark alignment, and short scope buffers in <see cref="AnalysisEngine"/>.</summary>
/// <remarks>Thread-safe with <c>ProcessAudio</c> and <c>GetSnapshot</c> (same lock as the engine). Used for Ctrl+R full layer reset.</remarks>
public interface IWaveformRetainedHistoryReset
{
    /// <summary>Clears retained waveform and scope data; capacity and configured history seconds are unchanged.</summary>
    void ResetRetainedWaveformHistory();
}
