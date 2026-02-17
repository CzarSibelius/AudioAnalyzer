using System.Text.Json;

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

    /// <summary>Layer-specific settings as JSON object. Only the owning layer deserializes this. Persisted as "Custom" in JSON.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("Custom")]
    public JsonElement? Custom { get; set; }

    /// <summary>Cache for GetCustom to avoid per-frame deserialization. Cleared when Custom changes.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    private Dictionary<RuntimeTypeHandle, (object? Value, JsonElement Snapshot)>? _customCache;

    /// <summary>Deserializes Custom to T. Returns null if Custom is empty or invalid. Results are cached until Custom changes.</summary>
    public T? GetCustom<T>() where T : class
    {
        if (Custom is null) { return null; }
        var value = Custom.Value;
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) { return null; }

        var key = typeof(T).TypeHandle;
        _customCache ??= new Dictionary<RuntimeTypeHandle, (object?, JsonElement)>();
        if (_customCache.TryGetValue(key, out var cached))
        {
            if (JsonElement.DeepEquals(value, cached.Snapshot))
            {
                return (T?)cached.Value;
            }
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<T>(value);
            _customCache[key] = (parsed, value);
            return parsed;
        }
        catch (JsonException)
        {
            /* Invalid custom JSON: return null to avoid crash; layer uses defaults */
            return null;
        }
    }

    /// <summary>Replaces Custom with the serialized form of the given object. Call after mutating custom settings.</summary>
    public void SetCustom<T>(T? value) where T : class
    {
        _customCache?.Clear();
        if (value is null)
        {
            Custom = null;
            return;
        }
        Custom = JsonSerializer.SerializeToElement(value);
    }

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

    /// <summary>Creates a deep copy of this layer settings instance.</summary>
    public TextLayerSettings DeepCopy()
    {
        return new TextLayerSettings
        {
            LayerType = LayerType,
            Enabled = Enabled,
            ZOrder = ZOrder,
            TextSnippets = new List<string>(TextSnippets),
            BeatReaction = BeatReaction,
            SpeedMultiplier = SpeedMultiplier,
            ColorIndex = ColorIndex,
            PaletteId = PaletteId,
            Custom = Custom
        };
    }
}
