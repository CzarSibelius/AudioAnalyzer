namespace AudioAnalyzer.Visualizers;

/// <summary>Provides per-layer animation state for stateful text layers. Each layer injects the store for its state type (e.g. ITextLayerStateStore&lt;BeatCirclesState&gt;).</summary>
/// <typeparam name="TState">State type for one layer kind (e.g. BeatCirclesState, FallingLettersLayerState). Must have a parameterless constructor.</typeparam>
public interface ITextLayerStateStore<TState> : ITextLayerStateStore
    where TState : new()
{
    /// <summary>Gets the mutable state for the given layer index. Creates and stores a new instance if the slot is empty or holds a different type.</summary>
    TState GetState(int layerIndex);

    /// <summary>Replaces the state for the given layer index.</summary>
    void SetState(int layerIndex, TState state);
}
