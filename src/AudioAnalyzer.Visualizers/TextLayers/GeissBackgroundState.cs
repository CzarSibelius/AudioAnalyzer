namespace AudioAnalyzer.Visualizers;

/// <summary>State for GeissBackground layer: phase, colorPhase, bass/treble intensity.</summary>
public sealed class GeissBackgroundState
{
    public double Phase { get; set; }
    public double ColorPhase { get; set; }
    public double BassIntensity { get; set; }
    public double TrebleIntensity { get; set; }
}
