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
        string? deviceNameRaw)
    {
        string deviceDisplay = FormatSettingValue(deviceNameRaw);
        string prefix = state.SelectedIndex == 0 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 0,
            "Audio input devices (D)",
            deviceDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the BPM source row (Audio / Demo / Ableton Link).</summary>
    public static string FormatBpmSourceLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        BpmSource source)
    {
        string value = source switch
        {
            BpmSource.AudioAnalysis => "Audio (beat detect)",
            BpmSource.DemoDevice => "Demo (time + demo device BPM)",
            BpmSource.AbletonLink => "Ableton Link",
            _ => source.ToString()
        };
        string prefix = state.SelectedIndex == 1 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 1,
            "BPM source (Enter)",
            value);
        return sb.ToString();
    }

    /// <summary>Formats the application name row for preformatted horizontal-row rendering.</summary>
    public static string FormatApplicationNameLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string appDisplay)
    {
        string prefix = state.SelectedIndex == 2 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 2,
            "Application name",
            appDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the UI theme row (layer palette id or custom).</summary>
    public static string FormatUiThemeLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string themeDisplay)
    {
        string prefix = state.SelectedIndex == 4 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 4,
            "UI theme (T)",
            themeDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the show-render-FPS toolbar overlay row (ADR-0067).</summary>
    public static string FormatShowRenderFpsLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        UiSettings uiSettings)
    {
        string value = uiSettings.ShowRenderFps ? "On" : "Off";
        string prefix = state.SelectedIndex == 5 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 5,
            "Show render FPS (Enter)",
            value);
        return sb.ToString();
    }

    /// <summary>Formats the default asset folder row for preformatted horizontal-row rendering.</summary>
    public static string FormatDefaultAssetFolderLine(
        GeneralSettingsHubState state,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        UiSettings uiSettings)
    {
        string valueDisplay = string.IsNullOrWhiteSpace(uiSettings.DefaultAssetFolderPath)
            ? "(App base)"
            : FormatSettingValue(uiSettings.DefaultAssetFolderPath);
        string prefix = state.SelectedIndex == 3 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(
            sb,
            palette,
            snapshot,
            beatColors,
            prefix,
            state.SelectedIndex == 3,
            "Default asset folder",
            valueDisplay);
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

    private static void AppendMenuLine(
        StringBuilder sb,
        UiPalette palette,
        AnalysisSnapshot snapshot,
        IReadOnlyList<PaletteColor> beatColors,
        string prefix,
        bool selected,
        string labelBeforeColon,
        string valueText)
    {
        var labelColor = selected ? palette.Highlighted : palette.Normal;
        AnsiConsole.AppendColored(sb, prefix, palette.Label);
        AnsiConsole.AppendColored(sb, labelBeforeColon, labelColor);
        AnsiConsole.AppendColored(sb, ":", labelColor);

        if (beatColors is { Count: > 0 })
        {
            int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snapshot, beatColors.Count);
            sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(valueText, beatColors, phase));
        }
        else
        {
            var valueColor = selected ? palette.Highlighted : palette.Dimmed;
            AnsiConsole.AppendColored(sb, valueText, valueColor);
        }
    }
}
