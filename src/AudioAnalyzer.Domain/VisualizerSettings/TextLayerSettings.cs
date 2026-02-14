namespace AudioAnalyzer.Domain;

/// <summary>Per-layer configuration for the layered text visualizer.</summary>
public class TextLayerSettings
{
    /// <summary>Kind of layer (e.g. ScrollingColors, Marquee).</summary>
    public TextLayerType LayerType { get; set; } = TextLayerType.Marquee;

    /// <summary>When false, the layer is not rendered. Default true.</summary>
    public bool Enabled { get; set; } = true;

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

    /// <summary>Amplitude gain for Oscilloscope layer (1.0–10.0). Default 2.5.</summary>
    public double Gain { get; set; } = 2.5;

    /// <summary>Show volume bar at top for LlamaStyle layer. Default false.</summary>
    public bool LlamaStyleShowVolumeBar { get; set; }

    /// <summary>Show row labels (100%, 75%, etc.) for LlamaStyle layer. Default false.</summary>
    public bool LlamaStyleShowRowLabels { get; set; }

    /// <summary>Show frequency labels (Hz) at bottom for LlamaStyle layer. Default false.</summary>
    public bool LlamaStyleShowFrequencyLabels { get; set; }

    /// <summary>Color scheme for LlamaStyle layer: "Winamp" (green→red) or "Spectrum" (red→blue). Default "Winamp".</summary>
    public string LlamaStyleColorScheme { get; set; } = "Winamp";

    /// <summary>Peak marker style for LlamaStyle layer: "Blocks" (▀▀) or "DoubleLine" (══). Default "Blocks".</summary>
    public string LlamaStylePeakMarkerStyle { get; set; } = "Blocks";

    /// <summary>Chars per band for LlamaStyle layer: 2 or 3. Default 3.</summary>
    public int LlamaStyleBarWidth { get; set; } = 3;

    /// <summary>Cycles the layer's type to the next value (wraps). Includes None.</summary>
    public static TextLayerType CycleTypeForward(TextLayerSettings layer)
    {
        var types = Enum.GetValues<TextLayerType>();
        int idx = Array.IndexOf(types, layer.LayerType);
        return types[(idx + 1) % types.Length];
    }

    /// <summary>Cycles the layer's type to the previous value (wraps). Includes None.</summary>
    public static TextLayerType CycleTypeBackward(TextLayerSettings layer)
    {
        var types = Enum.GetValues<TextLayerType>();
        int idx = Array.IndexOf(types, layer.LayerType);
        return types[(idx - 1 + types.Length) % types.Length];
    }
}
