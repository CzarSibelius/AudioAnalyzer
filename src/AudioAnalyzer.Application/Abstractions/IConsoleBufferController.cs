namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Adjusts the console screen buffer to match the rendered area. Implemented per platform
/// (Windows resizes the console buffer; other platforms are no-ops) and injected so shared
/// UI code does not branch on the operating system.
/// </summary>
public interface IConsoleBufferController
{
    /// <summary>
    /// Ensures the console buffer is at least the requested size when the platform supports it.
    /// Implementations ignore out-of-range requests and may no-op on platforms without a resizable buffer.
    /// </summary>
    void EnsureBufferSize(int width, int height);
}
