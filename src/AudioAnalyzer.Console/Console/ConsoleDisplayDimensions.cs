using System.IO;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Provides console window dimensions for layout and rendering.</summary>
public sealed class ConsoleDisplayDimensions : IDisplayDimensions
{
    private const int DefaultWidth = 80;
    private const int DefaultHeight = 24;

    public int Width => GetConsoleDimension(() => System.Console.WindowWidth, DefaultWidth);
    public int Height => GetConsoleDimension(() => System.Console.WindowHeight, DefaultHeight);

    private static int GetConsoleDimension(Func<int> getValue, int fallback)
    {
        try
        {
            return Math.Max(1, getValue());
        }
        catch (IOException)
        {
            return Math.Max(1, fallback);
        }
    }
}
