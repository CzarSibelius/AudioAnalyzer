namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides current display dimensions (e.g. console window size). Implemented by Infrastructure.
/// </summary>
public interface IDisplayDimensions
{
    int Width { get; }
    int Height { get; }
}
