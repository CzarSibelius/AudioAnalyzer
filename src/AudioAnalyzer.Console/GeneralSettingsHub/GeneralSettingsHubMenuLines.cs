using System.Collections.Generic;
using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Builds ANSI menu lines for the General Settings hub (label:value rows).</summary>
internal static class GeneralSettingsHubMenuLines
{
    /// <summary>
    /// Layer palette colors for the current UI theme when <see cref="UiSettings.UiThemePaletteId"/> is set;
    /// otherwise semantic <see cref="UiPalette"/> slots for beat/tick phase animation (Custom theme).
    /// </summary>
    public static IReadOnlyList<PaletteColor> ResolveHubBeatPaletteColors(
        UiSettings uiSettings,
        IPaletteRepository paletteRepo,
        UiPalette effectivePalette)
    {
        string? id = uiSettings.UiThemePaletteId;
        if (!string.IsNullOrWhiteSpace(id))
        {
            var def = paletteRepo.GetById(id.Trim());
            var colors = ColorPaletteParser.Parse(def);
            if (colors is { Count: > 0 })
            {
                return colors;
            }
        }

        return PaletteColorsFromUiPalette(effectivePalette);
    }

    /// <summary>Formats the audio input row for preformatted horizontal-row rendering.</summary>
    public static string FormatAudioLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string? deviceNameRaw,
        int rowDisplayWidth)
    {
        string deviceDisplay = FormatSettingValue(deviceNameRaw);
        if (state.SelectedIndex == 0)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "Audio input devices (D):" + deviceDisplay;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "Audio input devices (D)", deviceDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the BPM source row (Audio / Demo / Ableton Link).</summary>
    public static string FormatBpmSourceLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        BpmSource source,
        int rowDisplayWidth)
    {
        string value = source switch
        {
            BpmSource.AudioAnalysis => "Audio (beat detect)",
            BpmSource.DemoDevice => "Demo (time + demo device BPM)",
            BpmSource.AbletonLink => "Ableton Link",
            _ => source.ToString()
        };
        if (state.SelectedIndex == 1)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "BPM source (Enter):" + value;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "BPM source (Enter)", value);
        return sb.ToString();
    }

    /// <summary>Formats the application name row for preformatted horizontal-row rendering.</summary>
    public static string FormatApplicationNameLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string appDisplay,
        int rowDisplayWidth)
    {
        if (state.SelectedIndex == 2)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "Application name:" + appDisplay;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "Application name", appDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the UI theme row (layer palette id or custom).</summary>
    public static string FormatUiThemeLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string themeDisplay,
        int rowDisplayWidth)
    {
        if (state.SelectedIndex == 4)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "UI theme (T):" + themeDisplay;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "UI theme (T)", themeDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the show-render-FPS toolbar overlay row (ADR-0067).</summary>
    public static string FormatShowRenderFpsLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        UiSettings uiSettings,
        int rowDisplayWidth)
    {
        string value = uiSettings.ShowRenderFps ? "On" : "Off";
        if (state.SelectedIndex == 5)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "Show render FPS (Enter):" + value;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "Show render FPS (Enter)", value);
        return sb.ToString();
    }

    /// <summary>Formats the default asset folder row for preformatted horizontal-row rendering.</summary>
    public static string FormatDefaultAssetFolderLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        UiSettings uiSettings,
        int rowDisplayWidth)
    {
        string valueDisplay = string.IsNullOrWhiteSpace(uiSettings.DefaultAssetFolderPath)
            ? "(App base)"
            : FormatSettingValue(uiSettings.DefaultAssetFolderPath);
        if (state.SelectedIndex == 3)
        {
            var (selBg, selFg) = MenuSelectionAffordance.GetSelectionColors(palette);
            string inner = MenuSelectionAffordance.GetPrefix(true) + "Default asset folder:" + valueDisplay;
            return MenuSelectionAffordance.FormatAnsiSelectableRow(true, inner, rowDisplayWidth, selBg, selFg);
        }

        var sb = new StringBuilder();
        AppendMenuLineUnselected(sb, palette, snapshot, beatColors, "Default asset folder", valueDisplay);
        return sb.ToString();
    }

    /// <summary>Resolves display text for the current <see cref="UiSettings.UiThemePaletteId"/>.</summary>
    public static string ResolveUiThemeDisplaySummary(UiSettings uiSettings, IPaletteRepository paletteRepo)
    {
        string? id = uiSettings.UiThemePaletteId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return "(Custom)";
        }

        string trimmed = id.Trim();
        var def = paletteRepo.GetById(trimmed);
        var fromFile = def?.Name?.Trim();
        if (!string.IsNullOrEmpty(fromFile))
        {
            return fromFile;
        }

        foreach (var p in paletteRepo.GetAll())
        {
            if (string.Equals(p.Id, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(p.Name?.Trim()) ? p.Name!.Trim() : p.Id;
            }
        }

        return trimmed;
    }

    private static List<PaletteColor> PaletteColorsFromUiPalette(UiPalette effectivePalette)
    {
        var list = new List<PaletteColor>
        {
            effectivePalette.Normal,
            effectivePalette.Highlighted,
            effectivePalette.Dimmed,
            effectivePalette.Label
        };
        if (effectivePalette.Background is { } bg)
        {
            list.Add(bg);
        }

        return list;
    }

    private static string FormatSettingValue(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "\u2014" : raw.Trim();

    private static void AppendMenuLineUnselected(
        StringBuilder sb,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string labelBeforeColon,
        string valueText)
    {
        string prefix = MenuSelectionAffordance.GetPrefix(false);
        AnsiConsole.AppendColored(sb, prefix, palette.Label);
        AnsiConsole.AppendColored(sb, labelBeforeColon, palette.Normal);
        AnsiConsole.AppendColored(sb, ":", palette.Normal);

        if (beatColors is { Count: > 0 })
        {
            int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snapshot, beatColors.Count);
            sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(valueText, beatColors, phase));
        }
        else
        {
            AnsiConsole.AppendColored(sb, valueText, palette.Dimmed);
        }
    }
}
