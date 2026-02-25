namespace AudioAnalyzer.Domain;

/// <summary>
/// Application-defined limits for the TextLayers visualizer. Not user-editable (not in appsettings or settings UI). See ADR-0045.
/// </summary>
public static class TextLayersLimits
{
    /// <summary>
    /// Maximum number of text layers. Keys 1–9 map to layers 1–9. Default presets and padding use this count.
    /// </summary>
    public const int MaxLayerCount = 9;
}
