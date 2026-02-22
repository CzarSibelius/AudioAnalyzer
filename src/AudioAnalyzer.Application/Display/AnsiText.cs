using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Display text that may contain ANSI escape sequences (e.g. colors, styling).
/// Uses AnsiConsole for visible-length measurement and substring operations.
/// </summary>
public readonly struct AnsiText : IDisplayText
{
    /// <summary>The raw string value, possibly with embedded ANSI codes.</summary>
    public string Value { get; }

    /// <summary>Creates ANSI text from the given string.</summary>
    public AnsiText(string value)
    {
        Value = value ?? "";
    }

    /// <inheritdoc />
    public int GetVisibleLength() => AnsiConsole.GetVisibleLength(Value);

    /// <inheritdoc />
    public int GetDisplayWidth() => AnsiConsole.GetDisplayWidth(Value);

    /// <inheritdoc />
    public string PadToWidth(int width) => AnsiConsole.PadToVisibleWidth(Value, width);

    /// <inheritdoc />
    public string PadToDisplayWidth(int widthCols) => AnsiConsole.PadToDisplayWidth(Value, widthCols);

    /// <inheritdoc />
    public string GetVisibleSubstring(int startVisible, int widthVisible) =>
        AnsiConsole.GetVisibleSubstring(Value, startVisible, widthVisible);

    /// <inheritdoc />
    public string GetDisplaySubstring(int startCol, int widthCols) =>
        AnsiConsole.GetDisplaySubstring(Value, startCol, widthCols);

    /// <summary>Truncates to at most maxWidth display columns and appends "…" when exceeding. Preserves ANSI codes.</summary>
    public string TruncateWithEllipsis(int maxWidth)
    {
        if (string.IsNullOrEmpty(Value) || maxWidth <= 0)
        {
            return "";
        }

        int cols = GetDisplayWidth();
        if (cols <= maxWidth)
        {
            return Value;
        }

        if (maxWidth <= 1)
        {
            return "…";
        }

        return GetDisplaySubstring(0, maxWidth - 1) + "…";
    }

    /// <summary>Truncates to at most maxWidth display columns without ellipsis. Preserves ANSI codes.</summary>
    public string TruncateToWidth(int maxWidth)
    {
        if (string.IsNullOrEmpty(Value))
        {
            return "";
        }

        int cols = GetDisplayWidth();
        if (cols <= maxWidth)
        {
            return Value;
        }

        return GetDisplaySubstring(0, maxWidth);
    }
}
