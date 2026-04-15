namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Per-layer-slot cache for overview strip column→bucket indices. When width, bucket count, and visible chronology
/// match the previous frame, <see cref="WaveformStripLayer"/> reuses <see cref="OverviewColumnToBucket"/> instead of
/// recomputing <see cref="WaveformStripLayer.OverviewBucketIndexForColumn"/> for each column; min/max samples are still
/// read from the current snapshot every frame (ADR-0043).
/// </summary>
public sealed class WaveformStripLayerState
{
    /// <summary>Length equals last strip width; element <c>x</c> is the overview bucket index for column <c>x</c>.</summary>
    public int[]? OverviewColumnToBucket { get; set; }

    public int CachedUseLen { get; set; }

    public int CachedRingSampleCount { get; set; }

    public int CachedWindowSampleCount { get; set; }

    public int CachedOldestVisibleChronologicalIndex { get; set; }
}
