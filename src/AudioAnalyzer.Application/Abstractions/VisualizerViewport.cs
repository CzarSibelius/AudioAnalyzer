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

    /// <summary>
    /// Truncates a line to at most maxWidth characters and appends "…" when the text exceeds the width.
    /// Use for static text (titles, labels) where ellipsis indicates truncation. Per ADR-0020.
    /// </summary>
    public static string TruncateWithEllipsis(string line, int maxWidth)
    {
        if (string.IsNullOrEmpty(line) || maxWidth <= 0)
        {
            return "";
        }

        if (line.Length <= maxWidth)
        {
            return line;
        }

        if (maxWidth <= 1)
        {
            return "…";
        }

        return line[..(maxWidth - 1)] + "…";
    }
}
