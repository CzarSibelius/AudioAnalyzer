namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Beat circle for BeatCircles layer. ColorIndex is 0-5 (renderer maps to palette or ConsoleColor).
/// </summary>
public readonly struct BeatCircle
{
    public double Radius { get; }
    public double MaxRadius { get; }
    public int Age { get; }
    public int ColorIndex { get; }

    public BeatCircle(double radius, double maxRadius, int age, int colorIndex)
    {
        Radius = radius;
        MaxRadius = maxRadius;
        Age = age;
        ColorIndex = colorIndex;
    }
}
