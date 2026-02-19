using System.Globalization;
using System.Text;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Calculates terminal display width (columns) for text with wide characters (emoji, CJK).
/// Emoji and fullwidth characters occupy 2 columns; narrow characters occupy 1. Per Unicode UAX #11.
/// </summary>
public static class DisplayWidth
{
    /// <summary>Returns the display width (1 or 2) for the grapheme at the given index in plain text.</summary>
    public static int GetGraphemeWidth(string text, int index)
    {
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length)
        {
            return 0;
        }

        int v = Rune.GetRuneAt(text, index).Value;

        // East Asian Width W/F and common wide blocks: emoji, CJK, fullwidth
        if (v >= 0x1F300 && v <= 0x1FFFF) { return 2; }
        if (v >= 0x2600 && v <= 0x27BF) { return 2; }
        if (v >= 0xFE00 && v <= 0xFE6F) { return 2; }
        if (v >= 0xFF00 && v <= 0xFFEF) { return 2; }
        if (v >= 0x2E80 && v <= 0xA4CF) { return 2; }
        if (v >= 0xAC00 && v <= 0xD7A3) { return 2; }
        if (v >= 0xF900 && v <= 0xFAFF) { return 2; }
        if (v >= 0x1100 && v <= 0x115F) { return 2; }
        if (v >= 0x2329 && v <= 0x232A) { return 2; }
        if (v >= 0x2B50 && v <= 0x2B55) { return 2; }
        if (v >= 0x3030 && v <= 0x303D) { return 2; }
        if (v is 0x3297 or 0x3299) { return 2; }

        return 1;
    }

    /// <summary>Total display width of plain text (no ANSI). Iterates graphemes; wide graphemes count as 2.</summary>
    public static int GetDisplayWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int total = 0;
        int i = 0;
        while (i < text.Length)
        {
            total += GetGraphemeWidth(text, i);
            i += StringInfo.GetNextTextElementLength(text.AsSpan(i));
        }
        return total;
    }

    /// <summary>
    /// Snaps a column position to the start of the grapheme that contains it.
    /// When targetCol falls inside a wide character (e.g. column 1 of an emoji spanning 0-1),
    /// returns the grapheme start so we never land mid-emoji (avoids "two ticks" visual).
    /// </summary>
    public static int SnapToGraphemeStart(string text, int targetCol)
    {
        if (string.IsNullOrEmpty(text) || targetCol <= 0)
        {
            return 0;
        }

        int col = 0;
        int i = 0;
        while (i < text.Length)
        {
            int w = GetGraphemeWidth(text, i);
            if (targetCol < col + w)
            {
                return col;
            }
            col += w;
            i += StringInfo.GetNextTextElementLength(text.AsSpan(i));
        }
        return col;
    }
}
