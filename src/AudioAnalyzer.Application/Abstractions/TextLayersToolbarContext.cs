using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Context for building the TextLayers toolbar suffix string (layer digits, hints, gain, palette name).</summary>
public sealed class TextLayersToolbarContext
{
    /// <summary>Current analysis snapshot (e.g. for future toolbar data).</summary>
    public AnalysisSnapshot Snapshot { get; set; } = null!;

    /// <summary>Layers ordered by ZOrder.</summary>
    public IReadOnlyList<TextLayerSettings> SortedLayers { get; set; } = [];

    /// <summary>Current TextLayers config.</summary>
    public TextLayersVisualizerSettings? Settings { get; set; }

    /// <summary>Index of the layer whose palette is shown (1–<see cref="TextLayersLimits.MaxLayerCount"/>).</summary>
    public int PaletteCycleLayerIndex { get; set; }

    /// <summary>Palette repository to resolve palette names.</summary>
    public IPaletteRepository PaletteRepo { get; init; } = null!;

    /// <summary>UI palette and colors for toolbar styling.</summary>
    public UiSettings UiSettings { get; set; } = null!;

    /// <summary>When the selected layer is Oscilloscope, the gain to show in the toolbar. Set by the visualizer.</summary>
    public double? OscilloscopeGain { get; set; }
}
