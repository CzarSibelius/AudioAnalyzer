using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Builds effective <see cref="UiPalette"/> and <see cref="TitleBarPalette"/> from a theme file,
/// optional fallback layer palette mapping, and inline <see cref="UiSettings"/>.
/// </summary>
public static class UiThemeMerger
{
    /// <summary>
    /// Resolves base palettes: first tries <paramref name="fallbackPaletteId"/> via mapper; otherwise uses inline settings.
    /// </summary>
    public static (UiPalette Ui, TitleBarPalette TitleBar) ResolveBase(
        string? fallbackPaletteId,
        UiSettings uiSettings,
        IPaletteRepository paletteRepo)
    {
        ArgumentNullException.ThrowIfNull(uiSettings);
        ArgumentNullException.ThrowIfNull(paletteRepo);

        if (!string.IsNullOrWhiteSpace(fallbackPaletteId))
        {
            var def = paletteRepo.GetById(fallbackPaletteId.Trim());
            var colors = ColorPaletteParser.Parse(def);
            if (colors is { Count: > 0 })
            {
                return UiThemePaletteMapper.Map(colors);
            }
        }

        var inlineUi = uiSettings.Palette ?? new UiPalette();
        var inlineTb = uiSettings.TitleBarPalette ?? new TitleBarPalette();
        return (CloneUi(inlineUi), CloneTitleBar(inlineTb));
    }

    /// <summary>
    /// Overlays explicit theme sections onto <paramref name="baseUi"/> and <paramref name="baseTitleBar"/>.
    /// </summary>
    public static (UiPalette Ui, TitleBarPalette TitleBar) MergeOverlay(
        UiThemeDefinition theme,
        UiPalette baseUi,
        TitleBarPalette baseTitleBar)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentNullException.ThrowIfNull(baseUi);
        ArgumentNullException.ThrowIfNull(baseTitleBar);

        var ui = CloneUi(baseUi);
        var tb = CloneTitleBar(baseTitleBar);

        if (theme.Ui != null)
        {
            if (theme.Ui.Normal != null)
            {
                ui.Normal = ColorPaletteParser.ParseEntry(theme.Ui.Normal);
            }

            if (theme.Ui.Highlighted != null)
            {
                ui.Highlighted = ColorPaletteParser.ParseEntry(theme.Ui.Highlighted);
            }

            if (theme.Ui.Dimmed != null)
            {
                ui.Dimmed = ColorPaletteParser.ParseEntry(theme.Ui.Dimmed);
            }

            if (theme.Ui.Label != null)
            {
                ui.Label = ColorPaletteParser.ParseEntry(theme.Ui.Label);
            }

            if (theme.Ui.Background != null)
            {
                ui.Background = ColorPaletteParser.ParseEntry(theme.Ui.Background);
            }
        }

        if (theme.TitleBar != null)
        {
            if (theme.TitleBar.AppName != null)
            {
                tb.AppName = ColorPaletteParser.ParseEntry(theme.TitleBar.AppName);
            }

            if (theme.TitleBar.Mode != null)
            {
                tb.Mode = ColorPaletteParser.ParseEntry(theme.TitleBar.Mode);
            }

            if (theme.TitleBar.Preset != null)
            {
                tb.Preset = ColorPaletteParser.ParseEntry(theme.TitleBar.Preset);
            }

            if (theme.TitleBar.Layer != null)
            {
                tb.Layer = ColorPaletteParser.ParseEntry(theme.TitleBar.Layer);
            }

            if (theme.TitleBar.Separator != null)
            {
                tb.Separator = ColorPaletteParser.ParseEntry(theme.TitleBar.Separator);
            }

            if (theme.TitleBar.Frame != null)
            {
                tb.Frame = ColorPaletteParser.ParseEntry(theme.TitleBar.Frame);
            }
        }

        return (ui, tb);
    }

    private static UiPalette CloneUi(UiPalette source) =>
        new()
        {
            Normal = source.Normal,
            Highlighted = source.Highlighted,
            Dimmed = source.Dimmed,
            Label = source.Label,
            Background = source.Background
        };

    private static TitleBarPalette CloneTitleBar(TitleBarPalette source) =>
        new()
        {
            AppName = source.AppName,
            Mode = source.Mode,
            Preset = source.Preset,
            Layer = source.Layer,
            Separator = source.Separator,
            Frame = source.Frame
        };
}
