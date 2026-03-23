namespace AudioAnalyzer.Domain;

/// <summary>
/// Configurable UI settings: title, palette, scrolling speed. Persisted in appsettings.json.
/// </summary>
public class UiSettings
{
    /// <summary>Application title shown in the header. Default: "AUDIO ANALYZER - Real-time Frequency Spectrum".</summary>
    public string Title { get; set; } = "AUDIO ANALYZER - Real-time Frequency Spectrum";

    /// <summary>Optional short/stylized name for title bar (e.g. "aUdioNLZR"). When null/empty, derived from Title.</summary>
    public string? TitleBarAppName { get; set; }

    /// <summary>UI color palette (Normal, Highlighted, Dimmed, Label).</summary>
    public UiPalette Palette { get; set; } = new();

    /// <summary>Optional palette for title bar. When null, built-in cyberpunk defaults are used.</summary>
    public TitleBarPalette? TitleBarPalette { get; set; }

    /// <summary>
    /// When set, UI and title bar colors are derived from this palette file (same ids as layer palettes).
    /// When null/empty, <see cref="Palette"/> and <see cref="TitleBarPalette"/> from appsettings are used.
    /// </summary>
    public string? UiThemePaletteId { get; set; }

    /// <summary>Default scrolling speed (characters per frame) for ScrollingTextViewport. Default: 0.25.</summary>
    public double DefaultScrollingSpeed { get; set; } = 0.25;
}
