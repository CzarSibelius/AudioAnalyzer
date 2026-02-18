namespace AudioAnalyzer.Domain;

/// <summary>
/// Configurable UI settings: title, palette, scrolling speed. Persisted in appsettings.json.
/// </summary>
public class UiSettings
{
    /// <summary>Application title shown in the header. Default: "AUDIO ANALYZER - Real-time Frequency Spectrum".</summary>
    public string Title { get; set; } = "AUDIO ANALYZER - Real-time Frequency Spectrum";

    /// <summary>UI color palette (Normal, Highlighted, Dimmed, Label).</summary>
    public UiPalette Palette { get; set; } = new();

    /// <summary>Default scrolling speed (characters per frame) for ScrollingTextViewport. Default: 0.25.</summary>
    public double DefaultScrollingSpeed { get; set; } = 0.25;
}
