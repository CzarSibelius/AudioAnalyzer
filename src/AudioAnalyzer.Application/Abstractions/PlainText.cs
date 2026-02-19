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
    public int GetDisplayWidth() => DisplayWidth.GetDisplayWidth(Value);

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
    public string PadToDisplayWidth(int widthCols)
    {
        int cols = GetDisplayWidth();
        if (cols >= widthCols)
        {
            return Value;
        }
        return Value + new string(' ', widthCols - cols);
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

    /// <inheritdoc />
    public string GetDisplaySubstring(int startCol, int widthCols)
    {
        if (widthCols <= 0)
        {
            return "";
        }

        if (string.IsNullOrEmpty(Value))
        {
            return new string(' ', widthCols);
        }

        var sb = new System.Text.StringBuilder();
        int col = 0;
        int outCol = 0;
        int i = 0;

        while (i < Value.Length && outCol < widthCols)
        {
            int w = DisplayWidth.GetGraphemeWidth(Value, i);
            int elementLen = StringInfo.GetNextTextElementLength(Value.AsSpan(i));

            if (col >= startCol)
            {
                if (outCol + w > widthCols)
                {
                    break;
                }
                sb.Append(Value, i, elementLen);
                outCol += w;
            }

            col += w;
            i += elementLen;
        }

        int pad = widthCols - outCol;
        if (pad > 0)
        {
            sb.Append(' ', pad);
        }

        return sb.ToString();
    }

    /// <summary>Truncates to at most maxWidth display columns and appends "…" when exceeding. For static text per ADR-0020.</summary>
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

    /// <summary>Truncates to at most maxWidth display columns without ellipsis.</summary>
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
