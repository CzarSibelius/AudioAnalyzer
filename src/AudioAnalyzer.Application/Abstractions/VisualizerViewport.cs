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

    /// <summary>Truncates a line to at most maxWidth visible characters so it does not wrap.</summary>
    /// <param name="text">The text to truncate (plain or ANSI-styled).</param>
    /// <param name="maxWidth">Maximum visible width.</param>
    /// <returns>Truncated string, preserving ANSI codes when present.</returns>
    public static string TruncateToWidth<T>(T text, int maxWidth)
        where T : IDisplayText
    {
        return text.TruncateToWidth(maxWidth);
    }

    /// <summary>
    /// Truncates a line to at most maxWidth visible characters and appends "…" when the text exceeds the width.
    /// Use for static text (titles, labels) where ellipsis indicates truncation. Per ADR-0020.
    /// </summary>
    /// <param name="text">The text to truncate (plain or ANSI-styled).</param>
    /// <param name="maxWidth">Maximum visible width.</param>
    /// <returns>Truncated string with ellipsis if needed, preserving ANSI codes when present.</returns>
    public static string TruncateWithEllipsis<T>(T text, int maxWidth)
        where T : IDisplayText
    {
        return text.TruncateWithEllipsis(maxWidth);
    }
}
