using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface IVisualizationRenderer
{
    void Render(AnalysisSnapshot snapshot);

    /// <summary>Sets the palette for the visualizer. Call when palette selection changes.</summary>
    void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null);

    /// <summary>Whether the visualizer uses the global palette (P key).</summary>
    bool SupportsPaletteCycling();

    /// <summary>Handles visualizer-specific key bindings (e.g. 1–9 for TextLayers). Returns true if the key was consumed.</summary>
    bool HandleKey(ConsoleKeyInfo key);
}
