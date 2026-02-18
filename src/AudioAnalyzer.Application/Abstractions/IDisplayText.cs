namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Text that can be displayed in a fixed-width viewport. Implementations differ by whether
/// the text may contain ANSI escape sequences (colors, styling).
/// </summary>
public interface IDisplayText
{
    /// <summary>The raw string value.</summary>
    string Value { get; }

    /// <summary>Returns the number of visible (printed) characters, excluding any ANSI escape sequences.</summary>
    int GetVisibleLength();

    /// <summary>Pads the text so its visible length equals <paramref name="width"/>. Preserves embedded ANSI codes when present.</summary>
    string PadToWidth(int width);

    /// <summary>
    /// Returns the substring that displays exactly the visible characters in range [startVisible, startVisible + widthVisible).
    /// Preserves ANSI escape sequences when present; never cuts through an escape sequence.
    /// </summary>
    string GetVisibleSubstring(int startVisible, int widthVisible);

    /// <summary>Truncates to at most maxWidth visible characters without ellipsis.</summary>
    string TruncateToWidth(int maxWidth);

    /// <summary>Truncates to at most maxWidth visible characters and appends "…" when exceeding.</summary>
    string TruncateWithEllipsis(int maxWidth);
}
