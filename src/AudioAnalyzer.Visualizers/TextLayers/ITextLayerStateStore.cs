namespace AudioAnalyzer.Visualizers;

/// <summary>Provides capacity and clear operations for the shared per-slot state store. Used by TextLayersVisualizer for EnsureCapacity and clearing state when layer type changes.</summary>
public interface ITextLayerStateStore
{
    /// <summary>Ensures at least <paramref name="layerCount"/> slots exist. Call at the start of each render pass.</summary>
    void EnsureCapacity(int layerCount);

    /// <summary>Clears the state at the given layer index so the next draw will create state for the layer type at that slot.</summary>
    void ClearState(int layerIndex);
}
