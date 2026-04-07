using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context for the UI theme selection and authoring modal key handler.</summary>
internal sealed class UiThemeSelectionKeyContext : IKeyHandlerContext
{
    /// <summary>Active sub-flow.</summary>
    public UiThemeAuthoringPhase Phase { get; set; } = UiThemeAuthoringPhase.PickTheme;

    /// <summary>Themes from <c>themes/</c>; refreshed when returning from authoring.</summary>
    public IReadOnlyList<ThemeInfo> Themes { get; set; } = [];

    /// <summary>0 = (Custom); 1..Count map to <see cref="Themes"/>[i - 1].</summary>
    public int ThemeListSelectedIndex { get; set; }

    /// <summary>UI settings to update on confirm.</summary>
    public required UiSettings UiSettings { get; init; }

    /// <summary>Persists app settings after theme change.</summary>
    public required Action SaveSettings { get; init; }

    /// <summary>Theme file repository.</summary>
    public required IUiThemeRepository ThemeRepo { get; init; }

    /// <summary>Layer palette repository (source palettes + swatches).</summary>
    public required IPaletteRepository PaletteRepo { get; init; }

    /// <summary>Palettes when <see cref="Phase"/> is <see cref="UiThemeAuthoringPhase.NewPickPalette"/>.</summary>
    public IReadOnlyList<PaletteInfo>? NewPalettes { get; set; }

    /// <summary>Selected palette index for new theme.</summary>
    public int NewPaletteSelectedIndex { get; set; }

    /// <summary>Chosen palette id for slot editor.</summary>
    public string? SlotEditPaletteId { get; set; }

    /// <summary>Colors from chosen palette.</summary>
    public List<PaletteColor>? SlotEditPaletteColors { get; set; }

    /// <summary>Per-slot indices into <see cref="SlotEditPaletteColors"/> (length 11).</summary>
    public int[]? SlotEditIndices { get; set; }

    /// <summary>Focused row in slot editor (0..10 slots, 11 = save).</summary>
    public int SlotEditSelectedRow { get; set; }
}
