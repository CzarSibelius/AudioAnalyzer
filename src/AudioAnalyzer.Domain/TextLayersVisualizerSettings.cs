namespace AudioAnalyzer.Domain;

/// <summary>Settings for the layered text visualizer. Layers are drawn in ascending ZOrder.</summary>
public class TextLayersVisualizerSettings
{
    /// <summary>Ordered list of layers (sort by ZOrder when rendering).</summary>
    public List<TextLayerSettings> Layers { get; set; } = new();

    /// <summary>Id of the selected color palette (e.g. filename without extension). Resolved via IPaletteRepository.</summary>
    public string? PaletteId { get; set; }
}
