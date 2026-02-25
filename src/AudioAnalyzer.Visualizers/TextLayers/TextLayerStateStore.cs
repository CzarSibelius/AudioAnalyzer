namespace AudioAnalyzer.Visualizers;

/// <summary>Holds one state per layer slot; the state's type matches the layer type at that slot. Implements ITextLayerStateStore for capacity/clear and ITextLayerStateStore&lt;TState&gt; for each state type. Singleton; injected into TextLayersVisualizer and into layers that need it.</summary>
public sealed class TextLayerStateStore
    : ITextLayerStateStore<FallingLettersLayerState>,
      ITextLayerStateStore<AsciiImageState>,
      ITextLayerStateStore<GeissBackgroundState>,
      ITextLayerStateStore<BeatCirclesState>,
      ITextLayerStateStore<UnknownPleasuresState>,
      ITextLayerStateStore<MaschineState>
{
    private readonly List<object?> _stateByLayer = new();

    /// <inheritdoc />
    public void EnsureCapacity(int layerCount)
    {
        while (_stateByLayer.Count < layerCount)
        {
            _stateByLayer.Add(null);
        }
    }

    /// <inheritdoc />
    public void ClearState(int layerIndex)
    {
        EnsureCapacity(layerIndex + 1);
        _stateByLayer[layerIndex] = null;
    }

    FallingLettersLayerState ITextLayerStateStore<FallingLettersLayerState>.GetState(int layerIndex) => GetOrCreate<FallingLettersLayerState>(layerIndex);
    void ITextLayerStateStore<FallingLettersLayerState>.SetState(int layerIndex, FallingLettersLayerState state) => Set(layerIndex, state);

    AsciiImageState ITextLayerStateStore<AsciiImageState>.GetState(int layerIndex) => GetOrCreate<AsciiImageState>(layerIndex);
    void ITextLayerStateStore<AsciiImageState>.SetState(int layerIndex, AsciiImageState state) => Set(layerIndex, state);

    GeissBackgroundState ITextLayerStateStore<GeissBackgroundState>.GetState(int layerIndex) => GetOrCreate<GeissBackgroundState>(layerIndex);
    void ITextLayerStateStore<GeissBackgroundState>.SetState(int layerIndex, GeissBackgroundState state) => Set(layerIndex, state);

    BeatCirclesState ITextLayerStateStore<BeatCirclesState>.GetState(int layerIndex) => GetOrCreate<BeatCirclesState>(layerIndex);
    void ITextLayerStateStore<BeatCirclesState>.SetState(int layerIndex, BeatCirclesState state) => Set(layerIndex, state);

    UnknownPleasuresState ITextLayerStateStore<UnknownPleasuresState>.GetState(int layerIndex) => GetOrCreate<UnknownPleasuresState>(layerIndex);
    void ITextLayerStateStore<UnknownPleasuresState>.SetState(int layerIndex, UnknownPleasuresState state) => Set(layerIndex, state);

    MaschineState ITextLayerStateStore<MaschineState>.GetState(int layerIndex) => GetOrCreate<MaschineState>(layerIndex);
    void ITextLayerStateStore<MaschineState>.SetState(int layerIndex, MaschineState state) => Set(layerIndex, state);

    private T GetOrCreate<T>(int layerIndex) where T : new()
    {
        EnsureCapacity(layerIndex + 1);
        if (_stateByLayer[layerIndex] is not T state)
        {
            state = new T();
            _stateByLayer[layerIndex] = state;
        }
        return state;
    }

    private void Set(int layerIndex, object? state)
    {
        EnsureCapacity(layerIndex + 1);
        _stateByLayer[layerIndex] = state;
    }
}
