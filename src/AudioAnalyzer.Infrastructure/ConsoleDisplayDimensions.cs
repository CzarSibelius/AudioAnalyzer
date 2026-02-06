using System.IO;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure;

public sealed class ConsoleDisplayDimensions : IDisplayDimensions
{
    private const int DefaultWidth = 80;
    private const int DefaultHeight = 24;

    public int Width => GetConsoleDimension(() => Console.WindowWidth, DefaultWidth);
    public int Height => GetConsoleDimension(() => Console.WindowHeight, DefaultHeight);

    private static int GetConsoleDimension(Func<int> getValue, int fallback)
    {
        try
        {
            return getValue();
        }
        catch (IOException)
        {
            return fallback;
        }
    }
}
