using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Renders the settings overlay modal content (frame, layer list, settings list, hint line).</summary>
internal interface ISettingsModalRenderer
{
    /// <summary>Draws the full overlay content (title, hint, layer list, settings panel).</summary>
    /// <param name="state">Current modal state (focus, selection, buffers).</param>
    /// <param name="sortedLayers">Layers ordered by ZOrder.</param>
    /// <param name="width">Console width in columns.</param>
    void Draw(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width);

    /// <summary>Draws only the hint line at row 3. Used for scroll tick without full redraw.</summary>
    /// <param name="state">Current modal state.</param>
    /// <param name="width">Console width in columns.</param>
    void DrawHintLine(SettingsModalState state, int width);
}
