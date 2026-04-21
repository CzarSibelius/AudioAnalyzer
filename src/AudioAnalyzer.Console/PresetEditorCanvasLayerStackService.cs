using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <inheritdoc />
internal sealed class PresetEditorCanvasLayerStackService : IPresetEditorCanvasLayerStackService
{
    private readonly IDefaultTextLayersSettingsFactory _defaultTextLayersFactory;
    private readonly ITextLayerStateStore _layerStateStore;

    public PresetEditorCanvasLayerStackService(
        IDefaultTextLayersSettingsFactory defaultTextLayersFactory,
        ITextLayerStateStore layerStateStore)
    {
        _defaultTextLayersFactory = defaultTextLayersFactory ?? throw new ArgumentNullException(nameof(defaultTextLayersFactory));
        _layerStateStore = layerStateStore ?? throw new ArgumentNullException(nameof(layerStateStore));
    }

    /// <inheritdoc />
    public bool TryInsertLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer)
    {
        ArgumentNullException.ThrowIfNull(visualizerSettings);
        ArgumentNullException.ThrowIfNull(visualizer);

        var textLayers = visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
        visualizerSettings.TextLayers = textLayers;
        var layers = textLayers.Layers ??= new List<TextLayerSettings>();
        if (layers.Count >= TextLayersLimits.MaxLayerCount)
        {
            return false;
        }

        int maxZ = layers.Count > 0 ? layers.Max(l => l.ZOrder) : -1;
        int displayNum = layers.Count + 1;
        layers.Add(_defaultTextLayersFactory.CreatePaddingMarqueeLayer(maxZ + 1, displayNum));

        visualizer.OnTextLayersStructureChanged();
        int sortedCount = layers.OrderBy(l => l.ZOrder).Count();
        visualizer.SetActiveSortedLayerIndex(Math.Max(0, sortedCount - 1));
        return true;
    }

    /// <inheritdoc />
    public bool TryDeleteActiveLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer)
    {
        ArgumentNullException.ThrowIfNull(visualizerSettings);
        ArgumentNullException.ThrowIfNull(visualizer);

        var textLayers = visualizerSettings.TextLayers ?? new TextLayersVisualizerSettings();
        visualizerSettings.TextLayers = textLayers;
        var layers = textLayers.Layers;
        if (layers is not { Count: > 0 })
        {
            return false;
        }

        var sorted = layers.OrderBy(l => l.ZOrder).ToList();
        int idx = visualizer.GetActiveLayerZIndex();
        if (idx < 0 || idx >= sorted.Count)
        {
            return false;
        }

        var toRemove = sorted[idx];
        layers.Remove(toRemove);
        _layerStateStore.RemoveSlotAt(idx);
        visualizer.OnTextLayersStructureChanged();
        return true;
    }
}
