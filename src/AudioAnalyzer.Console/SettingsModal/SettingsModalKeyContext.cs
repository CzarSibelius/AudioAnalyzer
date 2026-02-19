using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context passed to the settings modal key handler each key.</summary>
internal sealed class SettingsModalKeyContext
{
    /// <summary>Modal state (focus, selection, buffers). Mutated by the handler.</summary>
    public SettingsModalState State { get; set; } = new();

    /// <summary>Layers ordered by ZOrder. Handler may replace this list (e.g. after creating a new preset).</summary>
    public List<TextLayerSettings> SortedLayers { get; set; } = [];

    /// <summary>Current TextLayers config. Handler may mutate layer properties.</summary>
    public TextLayersVisualizerSettings TextLayers { get; set; } = new();

    /// <summary>Visualizer settings (presets, active preset id). Handler may mutate.</summary>
    public VisualizerSettings VisualizerSettings { get; set; } = null!;

    /// <summary>Preset repository for rename/create.</summary>
    public IPresetRepository PresetRepository { get; init; } = null!;

    /// <summary>Called when settings should be persisted.</summary>
    public Action SaveSettings { get; init; } = () => { };
}
