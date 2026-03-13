using System.IO;

namespace AudioAnalyzer.Console;

/// <summary>Provides console dimension helpers. Registered as IConsoleDimensions for dependency injection per ADR-0040.</summary>
internal sealed class ConsoleDimensions : IConsoleDimensions
{
    /// <inheritdoc />
    public int GetConsoleWidth()
    {
        try
        {
            return System.Console.WindowWidth;
        }
        catch (IOException)
        {
            return 80;
        }
    }
}
