namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Abstracts writing a line of text to a console row. Implemented by the Console project
/// so that Application and Visualizers do not depend on System.Console. Per ADR-0026.
/// </summary>
public interface IConsoleWriter
{
    /// <summary>Writes a line of text at the given row. Column 0 is implied.</summary>
    /// <param name="row">Console row (0-based).</param>
    /// <param name="line">The line content (may contain ANSI escape sequences).</param>
    void WriteLine(int row, string line);
}
