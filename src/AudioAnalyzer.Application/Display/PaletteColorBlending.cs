using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Converts <see cref="PaletteColor"/> to RGB for mixing and blends colors. ConsoleColor entries use a fixed
/// VGA-style table (approximate terminal colors); RGB entries use their stored components.
/// </summary>
public static class PaletteColorBlending
{
    /// <summary>Approximate RGB for each <see cref="ConsoleColor"/> value (order matches enum 0–15).</summary>
    private static readonly (byte R, byte G, byte B)[] s_consoleColorRgb =
    [
        (0, 0, 0),       // Black
        (0, 0, 128),     // DarkBlue
        (0, 128, 0),     // DarkGreen
        (0, 128, 128),   // DarkCyan
        (128, 0, 0),     // DarkRed
        (128, 0, 128),   // DarkMagenta
        (128, 128, 0),   // DarkYellow
        (192, 192, 192), // Gray
        (128, 128, 128), // DarkGray
        (0, 0, 255),     // Blue
        (0, 255, 0),     // Green
        (0, 255, 255),   // Cyan
        (255, 0, 0),     // Red
        (255, 0, 255),   // Magenta
        (255, 255, 0),   // Yellow
        (255, 255, 255)  // White
    ];

    /// <summary>Returns 24-bit RGB for the palette color (direct or via console lookup).</summary>
    public static (byte R, byte G, byte B) ToRgb(PaletteColor color)
    {
        if (color.IsRgb)
        {
            return (color.R, color.G, color.B);
        }

        int i = (int)color.ConsoleColor!.Value;
        if ((uint)i >= s_consoleColorRgb.Length)
        {
            return (0, 0, 0);
        }

        return s_consoleColorRgb[i];
    }

    /// <summary>Linearly interpolates between two RGB triples; <paramref name="t"/> in [0, 1].</summary>
    public static (byte R, byte G, byte B) LerpRgb(
        (byte R, byte G, byte B) a,
        (byte R, byte G, byte B) b,
        double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        byte R = (byte)Math.Round(a.R + (b.R - a.R) * t);
        byte G = (byte)Math.Round(a.G + (b.G - a.G) * t);
        byte B = (byte)Math.Round(a.B + (b.B - a.B) * t);
        return (R, G, B);
    }

    /// <summary>Blends <paramref name="over"/> on top of <paramref name="under"/>; <paramref name="strength"/> in [0, 1].</summary>
    public static PaletteColor BlendOver(PaletteColor under, PaletteColor over, double strength)
    {
        strength = Math.Clamp(strength, 0.0, 1.0);
        var u = ToRgb(under);
        var o = ToRgb(over);
        var m = LerpRgb(u, o, strength);
        return PaletteColor.FromRgb(m.R, m.G, m.B);
    }
}
