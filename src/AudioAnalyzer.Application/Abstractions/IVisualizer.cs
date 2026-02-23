namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Renders the visualization from an analysis snapshot. The application uses a single implementation (e.g. TextLayersVisualizer).
/// Implementations hold their own state. Must not write more than viewport.MaxLines lines and no line longer than viewport.Width.
/// </summary>
public interface IVisualizer
{
    /// <summary>Whether the visualizer uses the global palette (P key).</summary>
    bool SupportsPaletteCycling { get; }

    void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport);

    /// <summary>Optional toolbar suffix (e.g. "Gain: 2.5 ([ ])"). Return null to use default toolbar only.</summary>
    string? GetToolbarSuffix(AnalysisSnapshot snapshot) => null;

    /// <summary>Optional: display name of the active/selected layer for title bar (e.g. "ascii_image"). Return null if not applicable.</summary>
    string? GetActiveLayerDisplayName() => null;

    /// <summary>Optional: 0-based z-order index of the active layer for title bar (e.g. 0 for back, 8 for front). Return -1 if not applicable.</summary>
    int GetActiveLayerZIndex() => -1;

    /// <summary>Handles visualizer-specific key bindings (e.g. 1–9 for TextLayers). Returns true if the key was consumed.</summary>
    bool HandleKey(ConsoleKeyInfo key) => false;
}
