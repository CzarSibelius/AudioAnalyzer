namespace AudioAnalyzer.Domain;

/// <summary>Title bar palette slots stored in a theme file.</summary>
public sealed class UiThemeTitleBarSection
{
    /// <summary>App name segment color.</summary>
    public PaletteColorEntry? AppName { get; set; }

    /// <summary>Mode segment color.</summary>
    public PaletteColorEntry? Mode { get; set; }

    /// <summary>Preset segment color.</summary>
    public PaletteColorEntry? Preset { get; set; }

    /// <summary>Layer segment color.</summary>
    public PaletteColorEntry? Layer { get; set; }

    /// <summary>Separator color.</summary>
    public PaletteColorEntry? Separator { get; set; }

    /// <summary>Frame color.</summary>
    public PaletteColorEntry? Frame { get; set; }
}
