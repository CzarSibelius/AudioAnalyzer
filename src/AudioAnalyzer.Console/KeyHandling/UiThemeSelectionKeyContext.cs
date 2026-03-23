using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Mutable context for the UI theme selection modal key handler.</summary>
internal sealed class UiThemeSelectionKeyContext : IKeyHandlerContext
{
    /// <summary>Repository palettes in stable order; index 0 in the UI list is (Custom), not this list.</summary>
    public required IReadOnlyList<PaletteInfo> Palettes { get; init; }

    /// <summary>0 = (Custom); 1..Count map to <see cref="Palettes"/>[i-1].</summary>
    public int SelectedIndex { get; set; }

    /// <summary>UI settings to update on confirm.</summary>
    public required UiSettings UiSettings { get; init; }

    /// <summary>Persists app settings after theme change.</summary>
    public required Action SaveSettings { get; init; }
}
