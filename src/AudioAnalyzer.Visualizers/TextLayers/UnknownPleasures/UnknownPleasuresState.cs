namespace AudioAnalyzer.Visualizers;

/// <summary>State for UnknownPleasures layer: snapshots, live pulse, beat tracking, and color offset.</summary>
public sealed class UnknownPleasuresState
{
    private const int SnapshotWidth = 120;
    private const int MaxSnapshots = 14;

    /// <summary>Beat-triggered frozen snapshots (up to 14).</summary>
    public List<double[]> Snapshots { get; } = new(MaxSnapshots);

    /// <summary>Realtime pulse buffer for the bottom line.</summary>
    public double[] LivePulse { get; } = new double[SnapshotWidth];

    /// <summary>Last beat count for snapshot capture detection.</summary>
    public int LastBeatCount { get; set; } = -1;

    /// <summary>Color offset for palette cycling on beat.</summary>
    public int ColorOffset { get; set; }
}
