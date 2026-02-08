namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders one visualization mode from an analysis snapshot. Implementations hold their own state (e.g. Geiss phases).
/// Must not write more than viewport.MaxLines lines and no line longer than viewport.Width.
/// </summary>
public interface IVisualizer
{
    /// <summary>Stable key for settings and CLI (e.g. "geiss", "unknownpleasures").</summary>
    string TechnicalName { get; }
    /// <summary>Human-readable name for toolbar and help (e.g. "Geiss", "Unknown Pleasures").</summary>
    string DisplayName { get; }
    /// <summary>Whether the visualizer uses the global palette (P key).</summary>
    bool SupportsPaletteCycling { get; }

    void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport);

    /// <summary>Optional toolbar suffix for this mode (e.g. "Gain: 2.5 ([ ])"). Return null to use default toolbar only.</summary>
    string? GetToolbarSuffix(AnalysisSnapshot snapshot) => null;
}
