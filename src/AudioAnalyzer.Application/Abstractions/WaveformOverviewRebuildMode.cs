namespace AudioAnalyzer.Application.Abstractions;

/// <summary>How <see cref="AnalysisEngine"/> builds the decimated waveform overview each display gate (ADR-0077).</summary>
public enum WaveformOverviewRebuildMode
{
    /// <summary>Do not rebuild; clear overview snapshot fields until the next gate.</summary>
    Skip,

    /// <summary>Partition all valid mono samples into overview buckets (full retained window).</summary>
    FullRing,

    /// <summary>Partition only the newest mono tail (see <see cref="IWaveformOverviewRebuildPolicy"/>).</summary>
    TrailingWindow
}
