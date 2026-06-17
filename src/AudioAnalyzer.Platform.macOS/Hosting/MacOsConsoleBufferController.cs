using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.macOS.Hosting;

/// <summary>macOS has no resizable console buffer API; buffer sizing is a no-op.</summary>
public sealed class MacOsConsoleBufferController : IConsoleBufferController
{
    /// <inheritdoc />
    public void EnsureBufferSize(int width, int height)
    {
        // No console buffer resize on macOS terminals.
    }
}
