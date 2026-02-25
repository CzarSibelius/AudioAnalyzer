using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Base type for all text layer renderers. Provides the common contract (LayerType, Draw) so layers can be registered and stored in collections without a non-generic interface. Each layer also implements <see cref="ITextLayerRenderer{TState}"/> for the per-layer state type (ADR-0044).</summary>
public abstract class TextLayerRendererBase
{
    /// <summary>The layer type this renderer handles.</summary>
    public abstract TextLayerType LayerType { get; }

    /// <summary>Draws the layer into the buffer. Returns updated state (offset, snippet index).</summary>
    public abstract (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx);
}
