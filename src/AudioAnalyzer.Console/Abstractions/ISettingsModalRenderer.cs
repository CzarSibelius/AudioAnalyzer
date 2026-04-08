using AudioAnalyzer.Application.Abstractions;
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
    /// <param name="frame">Current frame (analysis for palette animation; optional layer render times).</param>
    void Draw(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, VisualizationFrameContext frame);

    /// <summary>
    /// Idle poll: redraws the hint line and, when visible, the Palette settings cell or the open palette picker list
    /// when beat/tick animation frame advances (same phase as toolbar). Batched in one synchronized-output frame to reduce flicker.
    /// </summary>
    void DrawIdleOverlayTick(SettingsModalState state, IReadOnlyList<TextLayerSettings> sortedLayers, int width, VisualizationFrameContext frame);
}
