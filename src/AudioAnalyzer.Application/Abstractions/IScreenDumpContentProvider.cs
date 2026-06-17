namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Supplies visible console text for screen dumps. Implemented per platform (Windows reads the
/// console screen buffer via Win32; other platforms may have no implementation) and injected so
/// shared code does not branch on the operating system. Tests inject fixed content.
/// </summary>
public interface IScreenDumpContentProvider
{
    /// <summary>Returns null when capture is unavailable or fails.</summary>
    string? ReadVisibleConsoleContent();
}
