using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>Builds <see cref="UiThemeDefinition"/> instances for persistence (e.g. theme authoring UI).</summary>
public static class UiThemeDefinitionBuilder
{
    /// <summary>
    /// Slot order matches <see cref="UiThemePaletteMapper"/>: UI 0–4, title bar 5–10.
    /// Each value is an index into <paramref name="paletteColors"/> (wrapped with modulo).
    /// </summary>
    public static UiThemeDefinition FromPaletteSlotIndices(
        string? displayName,
        string fallbackPaletteId,
        IReadOnlyList<PaletteColor> paletteColors,
        ReadOnlySpan<int> slotIndices)
    {
        ArgumentNullException.ThrowIfNull(paletteColors);
        if (paletteColors.Count == 0)
        {
            throw new ArgumentException("Palette must have at least one color.", nameof(paletteColors));
        }

        if (slotIndices.Length != 11)
        {
            throw new ArgumentException("Expected 11 slot indices (UI 5 + title bar 6).", nameof(slotIndices));
        }

        int k = paletteColors.Count;
        var idxCopy = new int[11];
        slotIndices.CopyTo(idxCopy);
        PaletteColor At(int slot) => paletteColors[(idxCopy[slot] % k + k) % k];

        return new UiThemeDefinition
        {
            Name = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim(),
            FallbackPaletteId = fallbackPaletteId.Trim(),
            Ui = new UiThemeUiSection
            {
                Normal = ColorPaletteParser.ToEntry(At(0)),
                Highlighted = ColorPaletteParser.ToEntry(At(1)),
                Dimmed = ColorPaletteParser.ToEntry(At(2)),
                Label = ColorPaletteParser.ToEntry(At(3)),
                Background = ColorPaletteParser.ToEntry(At(4))
            },
            TitleBar = new UiThemeTitleBarSection
            {
                AppName = ColorPaletteParser.ToEntry(At(5)),
                Mode = ColorPaletteParser.ToEntry(At(6)),
                Preset = ColorPaletteParser.ToEntry(At(7)),
                Layer = ColorPaletteParser.ToEntry(At(8)),
                Separator = ColorPaletteParser.ToEntry(At(9)),
                Frame = ColorPaletteParser.ToEntry(At(10))
            }
        };
    }
}
