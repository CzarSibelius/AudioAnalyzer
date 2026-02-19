namespace AudioAnalyzer.Domain;

/// <summary>
/// Color palette for the title bar breadcrumb. When null in UiSettings, built-in cyberpunk defaults are used.
/// </summary>
public class TitleBarPalette
{
    /// <summary>Color for the app name segment.</summary>
    public PaletteColor AppName { get; set; } = PaletteColor.FromRgb(0, 255, 255);

    /// <summary>Color for the mode segment (Preset/Show).</summary>
    public PaletteColor Mode { get; set; } = PaletteColor.FromRgb(255, 0, 255);

    /// <summary>Color for the preset name segment.</summary>
    public PaletteColor Preset { get; set; } = PaletteColor.FromRgb(0, 255, 128);

    /// <summary>Color for the layer type segment.</summary>
    public PaletteColor Layer { get; set; } = PaletteColor.FromRgb(255, 255, 0);

    /// <summary>Color for the path separator (/).</summary>
    public PaletteColor Separator { get; set; } = PaletteColor.FromRgb(100, 100, 100);

    /// <summary>Color for the title bar box frame.</summary>
    public PaletteColor Frame { get; set; } = PaletteColor.FromRgb(0, 200, 200);
}
