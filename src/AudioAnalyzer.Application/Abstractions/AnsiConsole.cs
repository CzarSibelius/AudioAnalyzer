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
}
