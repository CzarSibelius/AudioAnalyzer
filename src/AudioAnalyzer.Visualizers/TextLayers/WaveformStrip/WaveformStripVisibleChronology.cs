namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Chronological slice of the overview ring used to map strip columns to decimated buckets.
/// <see cref="RingSampleCount"/> is the partition size for <c>WaveformOverviewBucketIndex</c>;
/// columns span <see cref="WindowSampleCount"/> samples starting at <see cref="OldestVisibleChronologicalIndex"/> (0 when the whole built partition is visible).
/// </summary>
public readonly record struct WaveformStripVisibleChronology(
    int RingSampleCount,
    int WindowSampleCount,
    int OldestVisibleChronologicalIndex);
