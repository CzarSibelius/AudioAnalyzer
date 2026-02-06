namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders one visualization mode from an analysis snapshot. Implementations hold their own state (e.g. Geiss phases).
/// </summary>
public interface IVisualizer
{
    void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow);
}
