using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Writes lines to System.Console. Implements IConsoleWriter so that Application and Visualizers
/// do not depend on System.Console. Per ADR-0026, the Console project owns all console I/O.
/// </summary>
internal sealed class ConsoleWriter : IConsoleWriter
{
    /// <inheritdoc />
    public void WriteLine(int row, string line)
    {
        try
        {
            System.Console.SetCursorPosition(0, row);
            System.Console.Write(line);
        }
        catch (Exception ex)
        {
            _ = ex;
            /* Console unavailable (e.g. resize, redirected): swallow to avoid crash */
        }
    }
}
