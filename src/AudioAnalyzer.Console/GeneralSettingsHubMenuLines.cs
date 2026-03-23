using System.Text;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Builds ANSI menu lines for the General Settings hub (label:value rows).</summary>
internal static class GeneralSettingsHubMenuLines
{
    /// <summary>Formats the audio input row for preformatted horizontal-row rendering.</summary>
    public static string FormatAudioLine(GeneralSettingsHubState state, UiPalette palette, string? deviceNameRaw)
    {
        string deviceDisplay = FormatSettingValue(deviceNameRaw);
        string prefix = state.SelectedIndex == 0 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(sb, palette, prefix, state.SelectedIndex == 0, "Audio input devices (D)", deviceDisplay);
        return sb.ToString();
    }

    /// <summary>Formats the application name row for preformatted horizontal-row rendering.</summary>
    public static string FormatApplicationNameLine(GeneralSettingsHubState state, UiPalette palette, string appDisplay)
    {
        string prefix = state.SelectedIndex == 1 ? "> " : "  ";
        var sb = new StringBuilder();
        AppendMenuLine(sb, palette, prefix, state.SelectedIndex == 1, "Application name", appDisplay);
        return sb.ToString();
    }

    private static string FormatSettingValue(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? "\u2014" : raw.Trim();

    private static void AppendMenuLine(
        StringBuilder sb,
        UiPalette palette,
        string prefix,
        bool selected,
        string labelBeforeColon,
        string valueText)
    {
        var labelColor = selected ? palette.Highlighted : palette.Normal;
        var valueColor = selected ? palette.Highlighted : palette.Dimmed;
        AnsiConsole.AppendColored(sb, prefix, palette.Label);
        AnsiConsole.AppendColored(sb, labelBeforeColon, labelColor);
        AnsiConsole.AppendColored(sb, ":", labelColor);
        AnsiConsole.AppendColored(sb, valueText, valueColor);
    }
}
