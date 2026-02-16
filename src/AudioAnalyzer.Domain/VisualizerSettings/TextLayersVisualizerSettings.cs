using System.Linq;

namespace AudioAnalyzer.Domain;

/// <summary>Settings for the layered text visualizer. Layers are drawn in ascending ZOrder.</summary>
public class TextLayersVisualizerSettings
{
    /// <summary>Ordered list of layers (sort by ZOrder when rendering).</summary>
    public List<TextLayerSettings> Layers { get; set; } = new();

    /// <summary>Id of the selected color palette (e.g. filename without extension). Resolved via IPaletteRepository.</summary>
    public string? PaletteId { get; set; }

    /// <summary>Creates a deep copy of this settings instance.</summary>
    public TextLayersVisualizerSettings DeepCopy()
    {
        return new TextLayersVisualizerSettings
        {
            PaletteId = PaletteId,
            Layers = Layers.Select(l => l.DeepCopy()).ToList()
        };
    }

    /// <summary>Replaces this instance's content with a deep copy of the source. Preserves object identity for references.</summary>
    public void CopyFrom(TextLayersVisualizerSettings source)
    {
        PaletteId = source.PaletteId;
        Layers.Clear();
        foreach (var layer in source.Layers)
        {
            Layers.Add(layer.DeepCopy());
        }
    }
}
