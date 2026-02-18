namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders static text within a fixed width by truncating. Use for titles, labels, and other
/// seldom-changing text where scrolling would be unnecessary. Per ADR-0020.
/// </summary>
public static class StaticTextViewport
{
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
