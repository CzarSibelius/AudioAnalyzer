using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Context passed to layer renderers for drawing. Contains buffer, snapshot, palette, dimensions, and layer index. Per-layer animation state is obtained via <see cref="ITextLayerStateStore{TState}"/> (e.g. ITextLayerStateStore&lt;BeatCirclesState&gt;) injected into stateful layers (those implementing <see cref="ITextLayerRenderer{TState}"/> with a non-NoLayerState type).</summary>
/// <remarks>
/// When <see cref="AudioAnalyzer.Domain.TextLayerSettings.RenderBounds"/> is set, <see cref="Width"/> and <see cref="Height"/> are the pixel size of that rectangle (layer-local coordinates); <see cref="BufferOriginX"/> and <see cref="BufferOriginY"/> map local (0,0) to the top-left cell of the clip. <see cref="ViewportWidth"/> and <see cref="ViewportHeight"/> are always the full visualizer viewport (for normalized bounds and layers that need global coordinates).
/// </remarks>
public sealed class TextLayerDrawContext
{
    public required ViewportCellBuffer Buffer { get; init; }
    public required AnalysisSnapshot Snapshot { get; init; }
    public required IReadOnlyList<PaletteColor> Palette { get; init; }
    public required double SpeedBurst { get; init; }

    /// <summary>Full visualizer viewport width in cells (same as <see cref="Width"/> when there is no render-bounds clip).</summary>
    public required int ViewportWidth { get; init; }

    /// <summary>Full visualizer viewport height in cells (same as <see cref="Height"/> when there is no render-bounds clip).</summary>
    public required int ViewportHeight { get; init; }

    /// <summary>Layer-local draw region width (clip width when <c>RenderBounds</c> is set).</summary>
    public required int Width { get; init; }

    /// <summary>Layer-local draw region height (clip height when <c>RenderBounds</c> is set).</summary>
    public required int Height { get; init; }

    /// <summary>X offset from layer-local coordinates to buffer coordinates.</summary>
    public int BufferOriginX { get; init; }

    /// <summary>Y offset from layer-local coordinates to buffer coordinates.</summary>
    public int BufferOriginY { get; init; }

    public required int LayerIndex { get; init; }

    /// <summary>Writes a cell using layer-local coordinates.</summary>
    public void SetLocal(int lx, int ly, char c, PaletteColor color)
    {
        Buffer.Set(BufferOriginX + lx, BufferOriginY + ly, c, color);
    }

    /// <summary>Reads a cell using layer-local coordinates.</summary>
    public (char c, PaletteColor color) GetLocal(int lx, int ly)
    {
        return Buffer.Get(BufferOriginX + lx, BufferOriginY + ly);
    }
}
