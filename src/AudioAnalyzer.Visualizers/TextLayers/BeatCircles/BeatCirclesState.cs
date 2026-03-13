namespace AudioAnalyzer.Visualizers;

/// <summary>State for BeatCircles layer: circle list, last beat count for spawn detection, and smoothed bass intensity.</summary>
public sealed class BeatCirclesState
{
    public List<BeatCircle> Circles { get; } = new();
    public int LastBeatCount { get; set; } = -1;
    public double BassIntensity { get; set; }
}
