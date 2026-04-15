namespace AudioAnalyzer.Visualizers;

/// <summary>Holds one state per layer slot; the state's type matches the layer type at that slot. Implements ITextLayerStateStore for capacity/clear and ITextLayerStateStore&lt;TState&gt; for each state type. Singleton; injected into TextLayersVisualizer and into layers that need it.</summary>
public sealed class TextLayerStateStore
    : ITextLayerStateStore<FallingLettersLayerState>,
      ITextLayerStateStore<AsciiImageState>,
      ITextLayerStateStore<AsciiVideoState>,
      ITextLayerStateStore<GeissBackgroundState>,
      ITextLayerStateStore<FractalZoomState>,
      ITextLayerStateStore<BeatCirclesState>,
      ITextLayerStateStore<UnknownPleasuresState>,
      ITextLayerStateStore<MaschineState>,
      ITextLayerStateStore<AsciiModelState>,
      ITextLayerStateStore<BufferDistortionState>,
      ITextLayerStateStore<WaveformStripLayerState>
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

    /// <inheritdoc />
    public void RemoveSlotAt(int sortedLayerIndex)
    {
        if (sortedLayerIndex < 0 || sortedLayerIndex >= _stateByLayer.Count)
        {
            return;
        }

        _stateByLayer.RemoveAt(sortedLayerIndex);
    }

    /// <inheritdoc />
    public void ApplySlotPermutation(IReadOnlyList<int> oldIndexByNewSlot)
    {
        int n = oldIndexByNewSlot.Count;
        if (n == 0)
        {
            return;
        }

        EnsureCapacity(n);
        var copy = new object?[n];
        for (int i = 0; i < n; i++)
        {
            copy[i] = i < _stateByLayer.Count ? _stateByLayer[i] : null;
        }

        for (int j = 0; j < n; j++)
        {
            int src = oldIndexByNewSlot[j];
            _stateByLayer[j] = (uint)src < (uint)copy.Length ? copy[src] : null;
        }
    }

    /// <inheritdoc />
    public void ClearAllSlots() => _stateByLayer.Clear();

    FallingLettersLayerState ITextLayerStateStore<FallingLettersLayerState>.GetState(int layerIndex) => GetOrCreate<FallingLettersLayerState>(layerIndex);
    void ITextLayerStateStore<FallingLettersLayerState>.SetState(int layerIndex, FallingLettersLayerState state) => Set(layerIndex, state);

    AsciiImageState ITextLayerStateStore<AsciiImageState>.GetState(int layerIndex) => GetOrCreate<AsciiImageState>(layerIndex);
    void ITextLayerStateStore<AsciiImageState>.SetState(int layerIndex, AsciiImageState state) => Set(layerIndex, state);

    AsciiVideoState ITextLayerStateStore<AsciiVideoState>.GetState(int layerIndex) => GetOrCreate<AsciiVideoState>(layerIndex);
    void ITextLayerStateStore<AsciiVideoState>.SetState(int layerIndex, AsciiVideoState state) => Set(layerIndex, state);

    GeissBackgroundState ITextLayerStateStore<GeissBackgroundState>.GetState(int layerIndex) => GetOrCreate<GeissBackgroundState>(layerIndex);
    void ITextLayerStateStore<GeissBackgroundState>.SetState(int layerIndex, GeissBackgroundState state) => Set(layerIndex, state);

    FractalZoomState ITextLayerStateStore<FractalZoomState>.GetState(int layerIndex) => GetOrCreate<FractalZoomState>(layerIndex);
    void ITextLayerStateStore<FractalZoomState>.SetState(int layerIndex, FractalZoomState state) => Set(layerIndex, state);

    BeatCirclesState ITextLayerStateStore<BeatCirclesState>.GetState(int layerIndex) => GetOrCreate<BeatCirclesState>(layerIndex);
    void ITextLayerStateStore<BeatCirclesState>.SetState(int layerIndex, BeatCirclesState state) => Set(layerIndex, state);

    UnknownPleasuresState ITextLayerStateStore<UnknownPleasuresState>.GetState(int layerIndex) => GetOrCreate<UnknownPleasuresState>(layerIndex);
    void ITextLayerStateStore<UnknownPleasuresState>.SetState(int layerIndex, UnknownPleasuresState state) => Set(layerIndex, state);

    MaschineState ITextLayerStateStore<MaschineState>.GetState(int layerIndex) => GetOrCreate<MaschineState>(layerIndex);
    void ITextLayerStateStore<MaschineState>.SetState(int layerIndex, MaschineState state) => Set(layerIndex, state);

    AsciiModelState ITextLayerStateStore<AsciiModelState>.GetState(int layerIndex) => GetOrCreate<AsciiModelState>(layerIndex);
    void ITextLayerStateStore<AsciiModelState>.SetState(int layerIndex, AsciiModelState state) => Set(layerIndex, state);

    BufferDistortionState ITextLayerStateStore<BufferDistortionState>.GetState(int layerIndex) => GetOrCreate<BufferDistortionState>(layerIndex);
    void ITextLayerStateStore<BufferDistortionState>.SetState(int layerIndex, BufferDistortionState state) => Set(layerIndex, state);

    WaveformStripLayerState ITextLayerStateStore<WaveformStripLayerState>.GetState(int layerIndex) => GetOrCreate<WaveformStripLayerState>(layerIndex);
    void ITextLayerStateStore<WaveformStripLayerState>.SetState(int layerIndex, WaveformStripLayerState state) => Set(layerIndex, state);

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
