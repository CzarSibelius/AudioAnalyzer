using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

public interface IVisualizationRenderer
{
    /// <summary>Renders the toolbar and visualizer. Full-screen mode is read from injected display state.</summary>
    void Render(VisualizationFrameContext frame);

    /// <summary>Sets the palette for the visualizer. Call when palette selection changes.</summary>
    void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null);

    /// <summary>Whether the visualizer uses the global palette (P key).</summary>
    bool SupportsPaletteCycling();

    /// <summary>Handles visualizer-specific key bindings (e.g. 1–<see cref="TextLayersLimits.MaxLayerCount"/> for TextLayers). Returns true if the key was consumed.</summary>
    bool HandleKey(ConsoleKeyInfo key);

    /// <summary>Call when the text layer list may have been replaced (e.g. preset cycle, show entry). Lets the visualizer drop cached per-frame state.</summary>
    void NotifyTextLayersStructureChanged();
}
