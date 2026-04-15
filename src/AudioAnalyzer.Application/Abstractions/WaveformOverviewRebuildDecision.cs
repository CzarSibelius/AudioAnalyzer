namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Returned by <see cref="IWaveformOverviewRebuildPolicy"/> so <see cref="AnalysisEngine"/> can skip work or limit overview aggregation to a trailing mono window.</summary>
public readonly struct WaveformOverviewRebuildDecision
{
    /// <summary>Constructs a decision (use static factories when possible).</summary>
    public WaveformOverviewRebuildDecision(WaveformOverviewRebuildMode mode, int trailingMonoSamples)
    {
        Mode = mode;
        TrailingMonoSamples = trailingMonoSamples;
    }

    /// <summary>How to rebuild this gate.</summary>
    public WaveformOverviewRebuildMode Mode { get; }

    /// <summary>When <see cref="Mode"/> is <see cref="WaveformOverviewRebuildMode.TrailingWindow"/>, number of newest mono samples to bucket (engine clamps to valid count).</summary>
    public int TrailingMonoSamples { get; }

    /// <summary>No enabled consumer needs overview data this gate.</summary>
    public static WaveformOverviewRebuildDecision Skip() => new(WaveformOverviewRebuildMode.Skip, 0);

    /// <summary>Bucket the entire valid mono history window (same as a null policy).</summary>
    public static WaveformOverviewRebuildDecision FullRing() => new(WaveformOverviewRebuildMode.FullRing, 0);

    /// <summary>Bucket only the trailing mono tail; <paramref name="monoSamples"/> is clamped by the engine to <c>[2, validMonoSampleCount]</c>.</summary>
    public static WaveformOverviewRebuildDecision TrailingWindow(int monoSamples) =>
        new(WaveformOverviewRebuildMode.TrailingWindow, monoSamples);
}
