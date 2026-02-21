using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Mutable context passed to the TextLayers key handler each key. Caller fills snapshot and callbacks; handler may mutate PaletteCycleLayerIndex and layer settings.</summary>
public sealed class TextLayersKeyContext
{
    /// <summary>Layers ordered by ZOrder. References match config; handler may mutate layer properties.</summary>
    public IReadOnlyList<TextLayerSettings> SortedLayers { get; set; } = [];

    /// <summary>Current TextLayers config (e.g. default PaletteId).</summary>
    public TextLayersVisualizerSettings? Settings { get; set; }

    /// <summary>Index of the layer whose palette P cycles (1–9 select). Mutated by the handler.</summary>
    public int PaletteCycleLayerIndex { get; set; }

    /// <summary>Palette repository for P-key cycling.</summary>
    public IPaletteRepository PaletteRepo { get; init; } = null!;

    /// <summary>Called when I key advances AsciiImage snippet index for a layer.</summary>
    public Action<int> AdvanceSnippetIndex { get; init; } = _ => { };

    /// <summary>Called when Left/Right changes layer type so type-specific state can be cleared.</summary>
    public Action<int, TextLayerType> ClearLayerState { get; init; } = (_, _) => { };
}
