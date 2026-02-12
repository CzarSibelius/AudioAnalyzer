using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface IVisualizationRenderer
{
    void Render(AnalysisSnapshot snapshot, VisualizationMode mode);

    /// <summary>Sets the palette for a specific visualization mode. Call when palette selection changes.</summary>
    void SetPaletteForMode(VisualizationMode mode, IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null);

    /// <summary>Human-readable name for the given mode (toolbar, help).</summary>
    string GetDisplayName(VisualizationMode mode);
    /// <summary>Stable key for settings and CLI (e.g. "geiss").</summary>
    string GetTechnicalName(VisualizationMode mode);
    /// <summary>Whether the visualizer for this mode uses the global palette (P key).</summary>
    bool SupportsPaletteCycling(VisualizationMode mode);
    /// <summary>Resolves a settings/CLI key to a mode; returns null if unknown.</summary>
    VisualizationMode? GetModeFromTechnicalName(string key);

    /// <summary>Handles mode-specific key bindings (e.g. 1–9 for TextLayers). Returns true if the key was consumed.</summary>
    bool HandleKey(ConsoleKeyInfo key, VisualizationMode mode);
}
