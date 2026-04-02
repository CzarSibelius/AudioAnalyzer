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

    /// <summary>Optional toolbar as separate labeled viewports (e.g. Layers, contextual fields, Palette). When non-null and non-empty, used instead of <see cref="GetToolbarSuffix"/>.</summary>
    IReadOnlyList<LabeledValueDescriptor>? GetToolbarViewports(AnalysisSnapshot snapshot) => null;

    /// <summary>Optional toolbar suffix (legacy single-string form). Return null to use default toolbar only. Ignored when <see cref="GetToolbarViewports"/> returns a non-empty list.</summary>
    string? GetToolbarSuffix(AnalysisSnapshot snapshot) => null;

    /// <summary>Optional: display name of the active/selected layer for title bar (e.g. "ascii_image"). Return null if not applicable.</summary>
    string? GetActiveLayerDisplayName() => null;

    /// <summary>Optional: 0-based z-order index of the active layer for title bar (e.g. 0 for back, 8 for front). Return -1 if not applicable.</summary>
    int GetActiveLayerZIndex() => -1;

    /// <summary>Called when the text layer list length may have changed (e.g. add/remove in the S modal). Implementations should clamp internal selection indices.</summary>
    void OnTextLayersStructureChanged() { }

    /// <summary>Handles visualizer-specific key bindings (e.g. 1–9 for TextLayers). Returns true if the key was consumed.</summary>
    bool HandleKey(ConsoleKeyInfo key) => false;
}
