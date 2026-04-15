namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Chooses whether the analysis engine rebuilds the decimated waveform overview each ~50 ms gate, and whether aggregation
/// scans the full valid mono ring or only a trailing tail (performance for long history + fixed-visible strip).
/// </summary>
public interface IWaveformOverviewRebuildPolicy
{
    /// <summary>Selects skip, full-ring, or trailing-window overview aggregation for the current gate.</summary>
    /// <param name="validMonoSampleCount"><c>min(historyTotalWritten, historyCapacity)</c> at the gate.</param>
    /// <param name="sampleRateHz">Current capture sample rate (Hz).</param>
    WaveformOverviewRebuildDecision GetDecision(int validMonoSampleCount, int sampleRateHz);
}
