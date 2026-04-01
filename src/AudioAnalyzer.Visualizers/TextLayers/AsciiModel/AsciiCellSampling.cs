namespace AudioAnalyzer.Visualizers;

/// <summary>Staggered sample positions inside a terminal cell (normalized 0–1). Must match <c>tools/AsciiShapeTableGen</c>.</summary>
public static class AsciiCellSampling
{
    /// <summary>Number of Harri-style shape samples per cell.</summary>
    public const int SampleCount = 6;

    /// <summary>Horizontal position of each sample in cell space (0 = left, 1 = right).</summary>
    public static ReadOnlySpan<float> NormalizedX => new float[]
    {
        0.22f, 0.78f, 0.18f, 0.82f, 0.22f, 0.78f
    };

    /// <summary>Vertical position of each sample in cell space (0 = top, 1 = bottom).</summary>
    public static ReadOnlySpan<float> NormalizedY => new float[]
    {
        0.15f, 0.15f, 0.42f, 0.42f, 0.72f, 0.72f
    };
}
