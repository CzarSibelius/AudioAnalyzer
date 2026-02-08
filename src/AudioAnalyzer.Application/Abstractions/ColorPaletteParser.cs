using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Parses ColorPalette (color names) to ConsoleColor for rendering.
/// Invalid names fall back to a default color.
/// </summary>
public static class ColorPaletteParser
{
    private static readonly ConsoleColor DefaultFallback = ConsoleColor.Gray;

    /// <summary>Default palette for Unknown Pleasures visualizer (magenta, yellow, green, cyan, blue).</summary>
    public static readonly IReadOnlyList<ConsoleColor> DefaultUnknownPleasuresPalette = [
        ConsoleColor.Magenta, ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.Blue
    ];

    /// <summary>
    /// Parses the palette's ColorNames to an array of ConsoleColor.
    /// Returns null if palette is null or has no colors; invalid names use DefaultFallback.
    /// </summary>
    public static IReadOnlyList<ConsoleColor>? Parse(ColorPalette? palette)
    {
        if (palette?.ColorNames is not { Length: > 0 })
            return null;

        var result = new ConsoleColor[palette.ColorNames.Length];
        for (int i = 0; i < palette.ColorNames.Length; i++)
        {
            var name = palette.ColorNames[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                result[i] = DefaultFallback;
                continue;
            }
            result[i] = TryParseColor(name.Trim()) ?? DefaultFallback;
        }
        return result;
    }

    private static ConsoleColor? TryParseColor(string name)
    {
        if (Enum.TryParse<ConsoleColor>(name, ignoreCase: true, out var color))
            return color;
        return null;
    }
}
