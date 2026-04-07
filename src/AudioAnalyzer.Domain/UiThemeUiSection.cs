namespace AudioAnalyzer.Domain;

/// <summary>UI palette slots stored in a theme file.</summary>
public sealed class UiThemeUiSection
{
    /// <summary>Default UI text color.</summary>
    public PaletteColorEntry? Normal { get; set; }

    /// <summary>Highlighted/active UI text.</summary>
    public PaletteColorEntry? Highlighted { get; set; }

    /// <summary>Dimmed UI text.</summary>
    public PaletteColorEntry? Dimmed { get; set; }

    /// <summary>Label/header text.</summary>
    public PaletteColorEntry? Label { get; set; }

    /// <summary>Optional selection/modal background.</summary>
    public PaletteColorEntry? Background { get; set; }
}
