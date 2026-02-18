namespace AudioAnalyzer.Visualizers;

/// <summary>Result of ASCII conversion: character grid, brightness for palette mapping, and optional per-pixel RGB.</summary>
public sealed class AsciiFrame
{
    public char[,] Chars { get; }
    public byte[,] Brightness { get; }
    public int Width { get; }
    public int Height { get; }

    /// <summary>Per-pixel red channel. Null when using layer palette. Non-null with G and B when using image colors.</summary>
    public byte[,]? R { get; }

    /// <summary>Per-pixel green channel. Null when using layer palette.</summary>
    public byte[,]? G { get; }

    /// <summary>Per-pixel blue channel. Null when using layer palette.</summary>
    public byte[,]? B { get; }

    /// <summary>Whether this frame has per-pixel RGB data for ImageColors palette source.</summary>
    public bool HasRgb => R != null && G != null && B != null;

    public AsciiFrame(char[,] chars, byte[,] brightness, int width, int height, byte[,]? r = null, byte[,]? g = null, byte[,]? b = null)
    {
        Chars = chars;
        Brightness = brightness;
        Width = width;
        Height = height;
        R = r;
        G = g;
        B = b;
    }
}
