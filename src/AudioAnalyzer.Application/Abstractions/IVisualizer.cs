namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders one visualization mode from an analysis snapshot. Implementations hold their own state (e.g. Geiss phases).
/// Must not write more than viewport.MaxLines lines and no line longer than viewport.Width.
/// </summary>
public interface IVisualizer
{
    void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport);
}
