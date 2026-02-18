using System.Globalization;
using System.Text;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Builds console output with ANSI color codes so that whole lines can be written in one call,
/// avoiding thousands of Console.ForegroundColor + Console.Write calls per frame.
/// </summary>
public static class AnsiConsole
{
    private const string Reset = "\x1b[0m";

    private static readonly string[] ForegroundCodes =
    [
        "\x1b[30m",  // Black
        "\x1b[34m",  // DarkBlue
        "\x1b[32m",  // DarkGreen
        "\x1b[36m",  // DarkCyan
        "\x1b[31m",  // DarkRed
        "\x1b[35m",  // DarkMagenta
        "\x1b[33m",  // DarkYellow
        "\x1b[37m",  // Gray
        "\x1b[90m",  // DarkGray
        "\x1b[94m",  // Blue
        "\x1b[92m",  // Green
        "\x1b[96m",  // Cyan
        "\x1b[91m",  // Red
        "\x1b[95m",  // Magenta
        "\x1b[93m",  // Yellow
        "\x1b[97m"   // White
    ];

    private static readonly string[] BackgroundCodes =
    [
        "\x1b[40m",   // Black
        "\x1b[44m",   // DarkBlue
        "\x1b[42m",   // DarkGreen
        "\x1b[46m",   // DarkCyan
        "\x1b[41m",   // DarkRed
        "\x1b[45m",   // DarkMagenta
        "\x1b[43m",   // DarkYellow
        "\x1b[47m",   // Gray
        "\x1b[100m",  // DarkGray
        "\x1b[104m",  // Blue
        "\x1b[102m",  // Green
        "\x1b[106m",  // Cyan
        "\x1b[101m",  // Red
        "\x1b[105m",  // Magenta
        "\x1b[103m",  // Yellow
        "\x1b[107m"   // White
    ];

    /// <summary>Returns the ANSI escape sequence for the given foreground color.</summary>
    public static string ColorCode(ConsoleColor color) => ForegroundCodes[(int)color];

    /// <summary>Returns the ANSI escape sequence for the given palette color (16-color or 24-bit RGB).</summary>
    public static string ColorCode(PaletteColor color)
    {
        if (color.IsRgb)
        {
            return $"\x1b[38;2;{color.R};{color.G};{color.B}m";
        }

        return ForegroundCodes[(int)color.ConsoleColor!.Value];
    }

    /// <summary>Returns the ANSI background escape sequence for the given palette color.</summary>
    public static string BackgroundCode(PaletteColor color)
    {
        if (color.IsRgb)
        {
            return $"\x1b[48;2;{color.R};{color.G};{color.B}m";
        }

        return BackgroundCodes[(int)color.ConsoleColor!.Value];
    }

    /// <summary>Returns the ANSI reset sequence.</summary>
    public static string ResetCode => Reset;

    /// <summary>Wraps <paramref name="text"/> with the given color and reset. Use when building a line from segments.</summary>
    public static string ToAnsiString(string text, ConsoleColor color) =>
        ForegroundCodes[(int)color] + text + Reset;

    /// <summary>Appends a colored segment to the builder (color code + text + reset).</summary>
    public static void AppendColored(StringBuilder sb, string text, ConsoleColor color)
    {
        sb.Append(ForegroundCodes[(int)color]);
        sb.Append(text);
        sb.Append(Reset);
    }

    /// <summary>Appends a single character with color (color code + char + reset).</summary>
    public static void AppendColored(StringBuilder sb, char c, ConsoleColor color)
    {
        sb.Append(ForegroundCodes[(int)color]);
        sb.Append(c);
        sb.Append(Reset);
    }

    /// <summary>Appends a colored segment using a palette color (16-color or 24-bit).</summary>
    public static void AppendColored(StringBuilder sb, string text, PaletteColor color)
    {
        sb.Append(ColorCode(color));
        sb.Append(text);
        sb.Append(Reset);
    }

    /// <summary>Appends a single character with palette color (16-color or 24-bit).</summary>
    public static void AppendColored(StringBuilder sb, char c, PaletteColor color)
    {
        sb.Append(ColorCode(color));
        sb.Append(c);
        sb.Append(Reset);
    }

    /// <summary>
    /// Returns the number of visible (printed) characters, excluding ANSI escape sequences.
    /// Counts grapheme clusters so emoji and other multi-codepoint characters are not split.
    /// </summary>
    public static int GetVisibleLength(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int count = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                i += 2;
                while (i < text.Length && (text[i] is >= '0' and <= '9' or ';' or '?' or ' '))
                {
                    i++;
                }
                if (i < text.Length)
                {
                    i++;
                }
                continue;
            }

            count++;
            i += StringInfo.GetNextTextElementLength(text.AsSpan(i));
        }
        return count;
    }

    /// <summary>
    /// Pads the text with spaces so its visible length equals <paramref name="width"/>. Preserves embedded ANSI codes.
    /// </summary>
    public static string PadToVisibleWidth(string text, int width)
    {
        int visible = GetVisibleLength(text);
        if (visible >= width)
        {
            return text;
        }

        return text + new string(' ', width - visible);
    }

    /// <summary>
    /// Returns the substring that displays exactly the visible characters in range [startVisible, startVisible + widthVisible).
    /// Preserves ANSI escape sequences so colors are maintained; never cuts through an escape sequence or grapheme cluster.
    /// Pads with spaces to <paramref name="widthVisible"/> if the range extends past the end.
    /// </summary>
    public static string GetVisibleSubstring(string text, int startVisible, int widthVisible)
    {
        if (widthVisible <= 0)
        {
            return "";
        }

        var sb = new StringBuilder();
        int visibleIndex = 0;
        int visibleOutput = 0;
        string? pendingEscape = null;
        int i = 0;

        while (i < text.Length && visibleOutput < widthVisible)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                int escStart = i;
                i += 2;
                while (i < text.Length && (text[i] is >= '0' and <= '9' or ';' or '?' or ' '))
                {
                    i++;
                }
                if (i < text.Length)
                {
                    i++;
                }
                string escape = text[escStart..i];
                if (visibleIndex >= startVisible)
                {
                    if (pendingEscape != null)
                    {
                        sb.Append(pendingEscape);
                        pendingEscape = null;
                    }
                    sb.Append(escape);
                }
                else
                {
                    pendingEscape = escape;
                }
                continue;
            }

            int elementLen = StringInfo.GetNextTextElementLength(text.AsSpan(i));
            if (visibleIndex >= startVisible)
            {
                if (pendingEscape != null)
                {
                    sb.Append(pendingEscape);
                    pendingEscape = null;
                }
                sb.Append(text, i, elementLen);
                visibleOutput++;
            }
            else
            {
                pendingEscape = null;
            }

            visibleIndex++;
            i += elementLen;
        }

        while (visibleOutput < widthVisible)
        {
            sb.Append(Reset);
            sb.Append(' ', widthVisible - visibleOutput);
            visibleOutput = widthVisible;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Strips ANSI escape sequences from a string so the result contains only printable characters.
    /// Use before scrolling text that may contain embedded escape codes, to avoid cutting sequences in half.
    /// </summary>
    public static string StripEscapes(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] == '\x1b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                i += 2;
                while (i < text.Length && (text[i] is >= '0' and <= '9' or ';' or '?' or ' '))
                {
                    i++;
                }
                if (i < text.Length)
                {
                    i++;
                }
                continue;
            }

            sb.Append(text[i]);
            i++;
        }
        return sb.ToString();
    }
}
