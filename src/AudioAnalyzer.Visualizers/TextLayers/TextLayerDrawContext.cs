using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Context passed to layer renderers for drawing. Contains buffer, snapshot, palette, dimensions, and layer index. Per-layer animation state is obtained via <see cref="ITextLayerStateStore{TState}"/> (e.g. ITextLayerStateStore&lt;BeatCirclesState&gt;) injected into stateful layers (those implementing <see cref="ITextLayerRenderer{TState}"/> with a non-NoLayerState type).</summary>
public sealed class TextLayerDrawContext
{
    public required ViewportCellBuffer Buffer { get; init; }
    public required AnalysisSnapshot Snapshot { get; init; }
    public required IReadOnlyList<PaletteColor> Palette { get; init; }
    public required double SpeedBurst { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required int LayerIndex { get; init; }
}
