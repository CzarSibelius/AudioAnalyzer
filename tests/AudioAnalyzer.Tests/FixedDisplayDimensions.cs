using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Tests;

/// <summary>IDisplayDimensions that returns fixed values for reproducible tests.</summary>
public sealed class FixedDisplayDimensions : IDisplayDimensions
{
    public FixedDisplayDimensions(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
}
