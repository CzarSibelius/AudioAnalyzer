using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Runs engine waveform reset, clears all <see cref="ITextLayerStateStore"/> slots, and resets text-layer visualizer draw state.</summary>
internal sealed class LayerRuntimeResetCoordinator : ILayerRuntimeResetCoordinator
{
    private readonly IWaveformRetainedHistoryReset _waveformRetainedHistoryReset;
    private readonly ITextLayerStateStore _textLayerStateStore;
    private readonly IFullLayerRuntimeReset _fullLayerRuntimeReset;

    public LayerRuntimeResetCoordinator(
        IWaveformRetainedHistoryReset waveformRetainedHistoryReset,
        ITextLayerStateStore textLayerStateStore,
        IFullLayerRuntimeReset fullLayerRuntimeReset)
    {
        _waveformRetainedHistoryReset = waveformRetainedHistoryReset ?? throw new ArgumentNullException(nameof(waveformRetainedHistoryReset));
        _textLayerStateStore = textLayerStateStore ?? throw new ArgumentNullException(nameof(textLayerStateStore));
        _fullLayerRuntimeReset = fullLayerRuntimeReset ?? throw new ArgumentNullException(nameof(fullLayerRuntimeReset));
    }

    /// <inheritdoc />
    public void ResetAllLayerRuntimeCaches()
    {
        _waveformRetainedHistoryReset.ResetRetainedWaveformHistory();
        _textLayerStateStore.ClearAllSlots();
        _fullLayerRuntimeReset.ResetAllRuntimeState();
    }
}
