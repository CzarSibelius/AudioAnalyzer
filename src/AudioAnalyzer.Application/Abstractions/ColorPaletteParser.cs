using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Parses palette definitions (ColorPalette or PaletteDefinition) to PaletteColor for rendering.
/// Supports 16-color names and 24-bit hex/RGB. Invalid entries fall back to a default color.
/// </summary>
public static class ColorPaletteParser
{
    private static readonly ConsoleColor DefaultFallback = ConsoleColor.Gray;
    private static readonly PaletteColor DefaultPaletteColorFallback = PaletteColor.FromConsoleColor(DefaultFallback);

    /// <summary>Default palette for Unknown Pleasures visualizer (magenta, yellow, green, cyan, blue).</summary>
    public static readonly IReadOnlyList<PaletteColor> DefaultUnknownPleasuresPalette = [
        PaletteColor.FromConsoleColor(ConsoleColor.Magenta),
        PaletteColor.FromConsoleColor(ConsoleColor.Yellow),
        PaletteColor.FromConsoleColor(ConsoleColor.Green),
        PaletteColor.FromConsoleColor(ConsoleColor.Cyan),
        PaletteColor.FromConsoleColor(ConsoleColor.Blue)
    ];

    /// <summary>
    /// Parses the palette file definition to a list of PaletteColor.
    /// Returns null if definition is null or has no colors; invalid entries use default fallback.
    /// </summary>
    public static IReadOnlyList<PaletteColor>? Parse(PaletteDefinition? definition)
    {
        if (definition?.Colors is not { Length: > 0 })
        {
            return null;
        }

        var result = new PaletteColor[definition.Colors.Length];
        for (int i = 0; i < definition.Colors.Length; i++)
        {
            result[i] = ParseEntry(definition.Colors[i]);
        }

        return result;
    }

    /// <summary>
    /// Parses the legacy ColorPalette (console color names) to PaletteColor list.
    /// Returns null if palette is null or has no colors.
    /// </summary>
    public static IReadOnlyList<PaletteColor>? Parse(ColorPalette? palette)
    {
        if (palette?.ColorNames is not { Length: > 0 })
        {
            return null;
        }

        var result = new PaletteColor[palette.ColorNames.Length];
        for (int i = 0; i < palette.ColorNames.Length; i++)
        {
            var name = palette.ColorNames[i];
            if (string.IsNullOrWhiteSpace(name))
            {
                result[i] = DefaultPaletteColorFallback;
                continue;
            }
            var cc = TryParseConsoleColor(name.Trim());
            result[i] = cc.HasValue ? PaletteColor.FromConsoleColor(cc.Value) : DefaultPaletteColorFallback;
        }
        return result;
    }

    /// <summary>Parses a single palette file entry to one PaletteColor.</summary>
    public static PaletteColor ParseEntry(PaletteColorEntry? entry)
    {
        if (entry == null)
        {
            return DefaultPaletteColorFallback;
        }

        if (entry.R.HasValue && entry.G.HasValue && entry.B.HasValue)
        {
            byte r = (byte)Math.Clamp(entry.R.Value, 0, 255);
            byte g = (byte)Math.Clamp(entry.G.Value, 0, 255);
            byte b = (byte)Math.Clamp(entry.B.Value, 0, 255);
            return PaletteColor.FromRgb(r, g, b);
        }

        if (!string.IsNullOrWhiteSpace(entry.Value))
        {
            var s = entry.Value.Trim();
            if (s.StartsWith('#') && s.Length >= 7 && TryParseHex(s, out byte r, out byte g, out byte b))
            {
                return PaletteColor.FromRgb(r, g, b);
            }

            var cc = TryParseConsoleColor(s);
            if (cc.HasValue)
            {
                return PaletteColor.FromConsoleColor(cc.Value);
            }
        }

        return DefaultPaletteColorFallback;
    }

    private static ConsoleColor? TryParseConsoleColor(string name)
    {
        if (Enum.TryParse<ConsoleColor>(name, ignoreCase: true, out var color))
        {
            return color;
        }

        return null;
    }

    private static bool TryParseHex(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        if (hex.Length < 7)
        {
            return false;
        }

        if (hex[0] != '#')
        {
            return false;
        }

        return byte.TryParse(hex.AsSpan(1, 2), System.Globalization.NumberStyles.HexNumber, null, out r)
            && byte.TryParse(hex.AsSpan(3, 2), System.Globalization.NumberStyles.HexNumber, null, out g)
            && byte.TryParse(hex.AsSpan(5, 2), System.Globalization.NumberStyles.HexNumber, null, out b);
    }
}
