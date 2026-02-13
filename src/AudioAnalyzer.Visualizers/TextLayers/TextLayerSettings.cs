namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer configuration for the layered text visualizer.</summary>
public class TextLayerSettings
{
    /// <summary>Kind of layer (e.g. ScrollingColors, Marquee).</summary>
    public TextLayerType LayerType { get; set; } = TextLayerType.Marquee;

    /// <summary>Draw order; lower values are drawn first (back).</summary>
    public int ZOrder { get; set; }

    /// <summary>Text snippets used when the layer type is text-based. Can be empty for non-text layers.</summary>
    public List<string> TextSnippets { get; set; } = new();

    /// <summary>How a beat affects this layer.</summary>
    public TextLayerBeatReaction BeatReaction { get; set; } = TextLayerBeatReaction.None;

    /// <summary>Speed multiplier (e.g. scroll speed). Default 1.0.</summary>
    public double SpeedMultiplier { get; set; } = 1.0;

    /// <summary>Optional palette color index or start index for gradient. Used by some layer types.</summary>
    public int ColorIndex { get; set; }

    /// <summary>Id of the color palette for this layer (e.g. "default"). Falls back to TextLayers.PaletteId when null/empty.</summary>
    public string? PaletteId { get; set; }

    /// <summary>Path to folder containing images. Used when LayerType is AsciiImage.</summary>
    public string? ImageFolderPath { get; set; }

    /// <summary>Movement mode for AsciiImage layer. Default Scroll.</summary>
    public AsciiImageMovement AsciiImageMovement { get; set; } = AsciiImageMovement.Scroll;
}
