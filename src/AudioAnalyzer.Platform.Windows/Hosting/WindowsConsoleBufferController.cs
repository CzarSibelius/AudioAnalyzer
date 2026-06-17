using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Platform.Windows.Hosting;

/// <summary>Resizes the Windows console screen buffer to match the rendered area.</summary>
public sealed class WindowsConsoleBufferController : IConsoleBufferController
{
    /// <inheritdoc />
    public void EnsureBufferSize(int width, int height)
    {
        if (width >= 10 && height >= 15)
        {
            System.Console.BufferWidth = width;
            System.Console.BufferHeight = height;
        }
    }
}
