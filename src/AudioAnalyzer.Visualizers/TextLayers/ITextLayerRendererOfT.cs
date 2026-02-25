namespace AudioAnalyzer.Visualizers;

/// <summary>Marks the per-layer state type for this renderer. Stateful layers use their state type; stateless layers use NoLayerState. No members; the contract is on <see cref="TextLayerRendererBase"/>. See ADR-0044.</summary>
/// <typeparam name="TState">The type of per-layer animation state this layer uses, or NoLayerState if none.</typeparam>
public interface ITextLayerRenderer<TState>
{
}
