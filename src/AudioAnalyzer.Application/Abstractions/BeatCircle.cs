namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Beat circle for Geiss visualization. ColorIndex is 0-5 (renderer maps to ConsoleColor).
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
