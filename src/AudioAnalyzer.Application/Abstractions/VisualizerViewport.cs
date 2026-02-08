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

    /// <summary>Truncates a line to at most maxWidth characters so it does not wrap.</summary>
    public static string TruncateToWidth(string line, int maxWidth)
    {
        if (string.IsNullOrEmpty(line))
        {
            return "";
        }

        if (line.Length <= maxWidth)
        {
            return line;
        }

        return line[..maxWidth];
    }
}
