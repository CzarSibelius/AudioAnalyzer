using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface IVisualizationRenderer
{
    void Render(AnalysisSnapshot snapshot, VisualizationMode mode);

    /// <summary>Sets the current palette for palette-cycling visualizers. Call when palette selection changes.</summary>
    void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null);

    /// <summary>Sets the layered text visualizer config. Call when TextLayers settings are loaded or changed.</summary>
    void SetTextLayersSettings(TextLayersVisualizerSettings? settings);

    /// <summary>Human-readable name for the given mode (toolbar, help).</summary>
    string GetDisplayName(VisualizationMode mode);
    /// <summary>Stable key for settings and CLI (e.g. "geiss").</summary>
    string GetTechnicalName(VisualizationMode mode);
    /// <summary>Whether the visualizer for this mode uses the global palette (P key).</summary>
    bool SupportsPaletteCycling(VisualizationMode mode);
    /// <summary>Resolves a settings/CLI key to a mode; returns null if unknown.</summary>
    VisualizationMode? GetModeFromTechnicalName(string key);
}
