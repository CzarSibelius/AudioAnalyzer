using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface IVisualizationRenderer
{
    void Render(AnalysisSnapshot snapshot, VisualizationMode mode);
}
