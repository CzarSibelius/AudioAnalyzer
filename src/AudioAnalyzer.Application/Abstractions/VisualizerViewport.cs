namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Bounds for visualizer output. Visualizers must not write more than MaxLines lines
/// and no line longer than Width. StartRow is the console row where the visualizer begins.
/// </summary>
public readonly struct VisualizerViewport
{
    public int StartRow { get; }
    public int MaxLines { get; }
    public int Width { get; }

    public VisualizerViewport(int startRow, int maxLines, int width)
    {
        StartRow = startRow;
        MaxLines = maxLines;
        Width = width;
    }
}
