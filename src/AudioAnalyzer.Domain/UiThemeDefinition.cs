namespace AudioAnalyzer.Domain;

/// <summary>
/// JSON file model for a UI theme under <c>themes/</c>. Semantic UI and title-bar colors;
/// optional <see cref="FallbackPaletteId"/> supplies base mapping and hub swatch animation.
/// </summary>
public sealed class UiThemeDefinition
{
    /// <summary>Optional display name; when empty the theme id is shown.</summary>
    public string? Name { get; set; }

    /// <summary>Optional layer palette id for base colors (via <c>UiThemePaletteMapper</c>) and animated list swatches.</summary>
    public string? FallbackPaletteId { get; set; }

    /// <summary>Explicit UI chrome slot overrides.</summary>
    public UiThemeUiSection? Ui { get; set; }

    /// <summary>Explicit title bar slot overrides.</summary>
    public UiThemeTitleBarSection? TitleBar { get; set; }
}
