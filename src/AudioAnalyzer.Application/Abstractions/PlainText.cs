using System.Globalization;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Unformatted (plain) display text. No ANSI escape sequences. Uses grapheme clusters
/// so emoji and other multi-codepoint characters are not split during scrolling or truncation.
/// </summary>
public readonly struct PlainText : IDisplayText
{
    /// <summary>The raw string value.</summary>
    public string Value { get; }

    /// <summary>Creates plain text from the given string.</summary>
    public PlainText(string value)
    {
        Value = value ?? "";
    }

    /// <inheritdoc />
    public int GetVisibleLength()
    {
        if (string.IsNullOrEmpty(Value))
        {
            return 0;
        }

        return new StringInfo(Value).LengthInTextElements;
    }

    /// <inheritdoc />
    public string PadToWidth(int width)
    {
        int visible = GetVisibleLength();
        if (visible >= width)
        {
            return Value;
        }

        return Value + new string(' ', width - visible);
    }

    /// <inheritdoc />
    public string GetVisibleSubstring(int startVisible, int widthVisible)
    {
        if (widthVisible <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(Value))
        {
            return new string(' ', widthVisible);
        }

        var si = new StringInfo(Value);
        int totalElements = si.LengthInTextElements;
        int start = Math.Clamp(startVisible, 0, totalElements);
        int available = totalElements - start;
        if (available <= 0)
        {
            return new string(' ', widthVisible);
        }

        int take = Math.Min(available, widthVisible);
        string sub = si.SubstringByTextElements(start, take);
        int pad = widthVisible - take;
        return pad <= 0 ? sub : sub + new string(' ', pad);
    }

    /// <summary>Truncates to at most maxWidth visible characters and appends "…" when exceeding. For static text per ADR-0020.</summary>
    public string TruncateWithEllipsis(int maxWidth)
    {
        if (string.IsNullOrEmpty(Value) || maxWidth <= 0)
        {
            return "";
        }

        int visible = GetVisibleLength();
        if (visible <= maxWidth)
        {
            return Value;
        }

        if (maxWidth <= 1)
        {
            return "…";
        }

        var si = new StringInfo(Value);
        return si.SubstringByTextElements(0, maxWidth - 1) + "…";
    }

    /// <summary>Truncates to at most maxWidth visible characters without ellipsis.</summary>
    public string TruncateToWidth(int maxWidth)
    {
        if (string.IsNullOrEmpty(Value))
        {
            return "";
        }

        int visible = GetVisibleLength();
        if (visible <= maxWidth)
        {
            return Value;
        }

        return new StringInfo(Value).SubstringByTextElements(0, maxWidth);
    }
}
