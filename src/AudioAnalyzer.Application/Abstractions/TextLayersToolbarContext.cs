using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Context for building the TextLayers toolbar suffix string (layer digits, hints, contextual rows, palette name).</summary>
public sealed class TextLayersToolbarContext
{
    /// <summary>Current visualization frame (toolbar phase, terminal width).</summary>
    public VisualizationFrameContext Frame { get; set; } = null!;

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

    /// <summary>Optional rows for the palette-cycled layer (gain, file name, etc.). Set by the visualizer.</summary>
    public IReadOnlyList<LayerToolbarContextualRow> ActiveLayerContextualRows { get; set; } = [];

    /// <summary>Current top-level mode; <see cref="ApplicationMode.ShowPlay"/> uses a compact toolbar.</summary>
    public ApplicationMode ApplicationMode { get; set; } = ApplicationMode.PresetEditor;

    /// <summary>When in Show play, the active show display name.</summary>
    public string? ActiveShowName { get; set; }

    /// <summary>0-based current entry index within the show.</summary>
    public int ShowEntryIndex { get; set; }

    /// <summary>Number of entries in the active show.</summary>
    public int ShowEntryCount { get; set; }
}
