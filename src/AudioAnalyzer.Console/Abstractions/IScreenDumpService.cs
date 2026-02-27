namespace AudioAnalyzer.Console;

/// <summary>
/// Captures the current console screen content and writes it to a text file (ASCII screenshot).
/// Used for bug reports and AI agents. On unsupported platforms or failure, returns null.
/// </summary>
internal interface IScreenDumpService
{
    /// <summary>
    /// Dumps the visible console buffer to a timestamped file in the given directory.
    /// </summary>
    /// <param name="stripAnsi">When true, removes ANSI escape sequences so the file is plain ASCII.</param>
    /// <param name="directory">Directory for the file; when null, uses a default (e.g. screen-dumps) next to the executable.</param>
    /// <returns>The full path of the written file, or null if capture failed (e.g. non-Windows or API error).</returns>
    string? DumpToFile(bool stripAnsi = true, string? directory = null);
}
