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
    /// When set, UI and title bar colors are resolved from <c>themes/{id}.json</c> (see ADR-0071).
    /// When null/empty, <see cref="Palette"/> and <see cref="TitleBarPalette"/> from appsettings are used.
    /// </summary>
    public string? UiThemeId { get; set; }

    /// <summary>Default scrolling speed as character advance per reference frame at 60 Hz; actual advance scales with <c>FrameDeltaSeconds</c> (ADR-0072). Default: 0.25.</summary>
    public double DefaultScrollingSpeed { get; set; } = 0.25;

    /// <summary>
    /// Optional default folder for layer asset directories (AsciiImage, AsciiModel). When null/empty,
    /// the process content root (<c>AppContext.BaseDirectory</c>) is used as the global base; relative layer paths combine with that base.
    /// </summary>
    public string? DefaultAssetFolderPath { get; set; }

    /// <summary>When true, show smoothed main render FPS on the toolbar (ADR-0067).</summary>
    public bool ShowRenderFps { get; set; }

    /// <summary>When true, show each text layer’s last measured <c>Draw</c> time in the S modal (ADR-0073).</summary>
    public bool ShowLayerRenderTime { get; set; }
}
