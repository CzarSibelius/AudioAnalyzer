namespace AudioAnalyzer.Domain;

/// <summary>
/// A single color for palette use: either 16-color (ConsoleColor) or 24-bit RGB.
/// Used by palette-aware visualizers and ANSI output.
/// </summary>
public readonly struct PaletteColor
{
    private readonly byte _r, _g, _b;
    private readonly ConsoleColor? _consoleColor;

    /// <summary>True when this color is 24-bit RGB; false when it is a 16-color ConsoleColor.</summary>
    public bool IsRgb => !_consoleColor.HasValue;

    /// <summary>24-bit red (0–255). Only meaningful when <see cref="IsRgb"/> is true.</summary>
    public byte R => _r;

    /// <summary>24-bit green (0–255). Only meaningful when <see cref="IsRgb"/> is true.</summary>
    public byte G => _g;

    /// <summary>24-bit blue (0–255). Only meaningful when <see cref="IsRgb"/> is true.</summary>
    public byte B => _b;

    /// <summary>16-color console color. Only meaningful when <see cref="IsRgb"/> is false.</summary>
    public ConsoleColor? ConsoleColor => _consoleColor;

    private PaletteColor(byte r, byte g, byte b, ConsoleColor? consoleColor)
    {
        _r = r;
        _g = g;
        _b = b;
        _consoleColor = consoleColor;
    }

    /// <summary>Creates a 24-bit RGB palette color.</summary>
    public static PaletteColor FromRgb(byte r, byte g, byte b) => new(r, g, b, null);

    /// <summary>Creates a 16-color palette color from a ConsoleColor.</summary>
    public static PaletteColor FromConsoleColor(ConsoleColor c) => new(0, 0, 0, c);
}
