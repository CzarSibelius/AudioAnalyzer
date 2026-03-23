using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Console;

/// <summary>Palette setting row and palette picker list drawing shared by the settings modal overlay.</summary>
internal static class SettingsSurfacesPaletteDrawing
{
    /// <summary>Draws the palette picker in the right column (inherit + repository palettes) with scroll.</summary>
    public static void DrawPicker(
        IPaletteRepository paletteRepo,
        SettingsModalState state,
        int leftColWidth,
        int firstLayerRow,
        int visibleRows,
        int rightColWidth,
        PaletteColor selBg,
        PaletteColor selFg,
        AnalysisSnapshot analysisSnapshot)
    {
        var palettes = paletteRepo.GetAll();
        int count = 1 + palettes.Count;
        int scrollOffset = SettingsSurfacesListDrawing.ComputeListScrollOffset(state.PalettePickerSelectedIndex, count, visibleRows);

        for (int vi = 0; vi < visibleRows; vi++)
        {
            int i = scrollOffset + vi;
            System.Console.SetCursorPosition(leftColWidth + 1, firstLayerRow + vi);
            if (i >= count)
            {
                System.Console.Write(new string(' ', rightColWidth));
                continue;
            }

            bool selected = i == state.PalettePickerSelectedIndex;
            if (i == 0)
            {
                string plain = StaticTextViewport.TruncateWithEllipsis(new PlainText("(inherit)"), rightColWidth);
                string core = selected
                    ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + plain + AnsiConsole.ResetCode
                    : plain;
                System.Console.Write(AnsiConsole.PadToDisplayWidth(core, rightColWidth));
            }
            else
            {
                var p = palettes[i - 1];
                string displayName = !string.IsNullOrWhiteSpace(p.Name?.Trim()) ? p.Name!.Trim() : p.Id;
                string row = FormatPickerPaletteRow(
                    paletteRepo,
                    p.Id,
                    displayName,
                    rightColWidth,
                    selected,
                    selBg,
                    selFg,
                    analysisSnapshot);
                System.Console.Write(row);
            }
        }
    }

    /// <summary>One palette row in the picker list (colored name, optional selection highlight).</summary>
    public static string FormatPickerPaletteRow(
        IPaletteRepository paletteRepo,
        string paletteId,
        string displayName,
        int rightColWidth,
        bool selected,
        PaletteColor selBg,
        PaletteColor selFg,
        AnalysisSnapshot analysisSnapshot)
    {
        var def = paletteRepo.GetById(paletteId);
        var colors = ColorPaletteParser.Parse(def);
        string truncatedName = StaticTextViewport.TruncateWithEllipsis(new PlainText(displayName), rightColWidth);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysisSnapshot, colors?.Count ?? 0);
        string coloredName = PaletteSwatchFormatter.FormatPaletteColoredName(truncatedName, colors, phase);
        string core = selected
            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + coloredName + AnsiConsole.ResetCode
            : coloredName;
        return AnsiConsole.PadToDisplayWidth(core, rightColWidth);
    }

    /// <summary>Palette setting row in the settings column (label + colored effective palette name).</summary>
    public static string FormatPaletteSettingRow(
        IPaletteRepository paletteRepo,
        VisualizerSettings visualizerSettings,
        TextLayerSettings layer,
        int rightColWidth,
        bool selected,
        PaletteColor selBg,
        PaletteColor selFg,
        AnalysisSnapshot analysisSnapshot)
    {
        const string labelText = "Palette:";
        string namePart = ResolvePaletteDisplayName(paletteRepo, layer);
        string effectiveId = string.IsNullOrWhiteSpace(layer.PaletteId)
            ? (visualizerSettings.TextLayers?.PaletteId ?? "")
            : layer.PaletteId!;
        if (string.IsNullOrWhiteSpace(effectiveId))
        {
            effectiveId = "default";
        }

        var def = paletteRepo.GetById(effectiveId);
        var colors = ColorPaletteParser.Parse(def);
        int labelCols = DisplayWidth.GetDisplayWidth(labelText);
        int nameMaxCols = Math.Max(0, rightColWidth - labelCols);
        string truncatedName = nameMaxCols > 0
            ? StaticTextViewport.TruncateWithEllipsis(new PlainText(namePart), nameMaxCols)
            : "";
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysisSnapshot, colors?.Count ?? 0);
        string coloredName = PaletteSwatchFormatter.FormatPaletteColoredName(truncatedName, colors, phase);

        string core = selected
            ? AnsiConsole.BackgroundCode(selBg) + AnsiConsole.ColorCode(selFg) + labelText + AnsiConsole.ResetCode + coloredName
            : labelText + coloredName;

        return AnsiConsole.PadToDisplayWidth(core, rightColWidth);
    }

    private static string ResolvePaletteDisplayName(IPaletteRepository paletteRepo, TextLayerSettings layer)
    {
        if (string.IsNullOrWhiteSpace(layer.PaletteId))
        {
            return "(inherit)";
        }

        var def = paletteRepo.GetById(layer.PaletteId);
        var fromFile = def?.Name?.Trim();
        if (!string.IsNullOrEmpty(fromFile))
        {
            return fromFile;
        }

        foreach (var p in paletteRepo.GetAll())
        {
            if (string.Equals(p.Id, layer.PaletteId, StringComparison.OrdinalIgnoreCase))
            {
                return p.Name;
            }
        }

        return layer.PaletteId!;
    }
}
