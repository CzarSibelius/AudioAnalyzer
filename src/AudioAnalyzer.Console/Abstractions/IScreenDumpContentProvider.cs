namespace AudioAnalyzer.Console;

/// <summary>
/// Supplies visible console text for <see cref="IScreenDumpService"/>. Production uses Win32; tests inject fixed content.
/// </summary>
internal interface IScreenDumpContentProvider
{
    /// <summary>Returns null when capture is unavailable or fails.</summary>
    string? ReadVisibleConsoleContent();
}
