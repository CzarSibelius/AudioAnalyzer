namespace AudioAnalyzer.Visualizers;

/// <summary>Renderer for a single text layer type. Each layer type (ScrollingColors, Marquee, etc.) has its own implementation.</summary>
public interface ITextLayerRenderer
{
    TextLayerType LayerType { get; }

    /// <summary>Draws the layer into the buffer. Returns updated state (offset, snippet index).</summary>
    (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx);
}
