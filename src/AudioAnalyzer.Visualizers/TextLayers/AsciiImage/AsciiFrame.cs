namespace AudioAnalyzer.Visualizers;

/// <summary>Result of ASCII conversion: character grid and brightness for palette mapping.</summary>
public sealed class AsciiFrame
{
    public char[,] Chars { get; }
    public byte[,] Brightness { get; }
    public int Width { get; }
    public int Height { get; }

    public AsciiFrame(char[,] chars, byte[,] brightness, int width, int height)
    {
        Chars = chars;
        Brightness = brightness;
        Width = width;
        Height = height;
    }
}
