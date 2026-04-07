namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Beat circle for BeatCircles layer. ColorIndex is 0-5 (renderer maps to palette or ConsoleColor).
/// </summary>
public readonly struct BeatCircle
{
    public double Radius { get; }
    public double MaxRadius { get; }
    /// <summary>Age in reference-frame units (~60 Hz); removes near 30 for same lifetime as legacy frame count.</summary>
    public double Age { get; }
    public int ColorIndex { get; }

    public BeatCircle(double radius, double maxRadius, double age, int colorIndex)
    {
        Radius = radius;
        MaxRadius = maxRadius;
        Age = age;
        ColorIndex = colorIndex;
    }
}
