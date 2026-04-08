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
        AudioAnalysisSnapshot analysis)
    {
        DrawPicker(paletteRepo, state, leftColWidth, firstLayerRow, visibleRows, rightColWidth, selBg, selFg, analysis, includeInheritFirst: true);
    }

    /// <summary>Draws the palette picker: either <paramref name="includeInheritFirst"/> (inherit + repo) or preset-default mode (repo palettes only).</summary>
    public static void DrawPicker(
        IPaletteRepository paletteRepo,
        SettingsModalState state,
        int leftColWidth,
        int firstLayerRow,
        int visibleRows,
        int rightColWidth,
        PaletteColor selBg,
        PaletteColor selFg,
        AudioAnalysisSnapshot analysis,
        bool includeInheritFirst)
    {
        var palettes = paletteRepo.GetAll();
        int count = includeInheritFirst ? 1 + palettes.Count : palettes.Count;
        if (count == 0)
        {
            for (int vi = 0; vi < visibleRows; vi++)
            {
                System.Console.SetCursorPosition(leftColWidth + 1, firstLayerRow + vi);
                System.Console.Write(new string(' ', rightColWidth));
            }

            return;
        }

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
            if (includeInheritFirst && i == 0)
            {
                string prefix = MenuSelectionAffordance.GetPrefix(selected);
                int nameBudget = Math.Max(0, rightColWidth - MenuSelectionAffordance.PrefixDisplayWidth);
                string plain = StaticTextViewport.TruncateWithEllipsis(new PlainText("(inherit)"), nameBudget);
                string inner = prefix + plain;
                System.Console.Write(MenuSelectionAffordance.FormatAnsiSelectableRow(selected, inner, rightColWidth, selBg, selFg));
            }
            else
            {
                int pi = includeInheritFirst ? i - 1 : i;
                var p = palettes[pi];
                string displayName = !string.IsNullOrWhiteSpace(p.Name?.Trim()) ? p.Name!.Trim() : p.Id;
                string row = FormatPickerPaletteRow(
                    paletteRepo,
                    p.Id,
                    displayName,
                    rightColWidth,
                    selected,
                    selBg,
                    selFg,
                    analysis);
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
        AudioAnalysisSnapshot analysis)
    {
        var def = paletteRepo.GetById(paletteId);
        var colors = ColorPaletteParser.Parse(def);
        string prefix = MenuSelectionAffordance.GetPrefix(selected);
        int nameBudget = Math.Max(0, rightColWidth - MenuSelectionAffordance.PrefixDisplayWidth);
        string truncatedName = StaticTextViewport.TruncateWithEllipsis(new PlainText(displayName), nameBudget);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysis, colors?.Count ?? 0);
        string coloredName = PaletteSwatchFormatter.FormatPaletteColoredName(truncatedName, colors, phase);
        string inner = prefix + coloredName;
        return MenuSelectionAffordance.FormatAnsiSelectableRow(selected, inner, rightColWidth, selBg, selFg);
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
        AudioAnalysisSnapshot analysis)
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
        string prefix = MenuSelectionAffordance.GetPrefix(selected);
        string labelWithPrefix = prefix + labelText;
        int labelCols = DisplayWidth.GetDisplayWidth(labelWithPrefix);
        int nameMaxCols = Math.Max(0, rightColWidth - labelCols);
        string truncatedName = nameMaxCols > 0
            ? StaticTextViewport.TruncateWithEllipsis(new PlainText(namePart), nameMaxCols)
            : "";
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysis, colors?.Count ?? 0);
        string coloredName = PaletteSwatchFormatter.FormatPaletteColoredName(truncatedName, colors, phase);
        string inner = labelWithPrefix + coloredName;
        return MenuSelectionAffordance.FormatAnsiSelectableRow(selected, inner, rightColWidth, selBg, selFg);
    }

    /// <summary>Preset default palette row: <c>TextLayers.PaletteId</c> (falls back to <c>default</c> when empty).</summary>
    public static string FormatPresetDefaultPaletteSettingRow(
        IPaletteRepository paletteRepo,
        TextLayersVisualizerSettings textLayers,
        int rightColWidth,
        bool selected,
        PaletteColor selBg,
        PaletteColor selFg,
        AudioAnalysisSnapshot analysis)
    {
        const string labelText = "Default palette:";
        string effectiveId = textLayers.PaletteId ?? "";
        if (string.IsNullOrWhiteSpace(effectiveId))
        {
            effectiveId = "default";
        }

        string namePart = ResolvePresetDefaultPaletteDisplayName(paletteRepo, textLayers);
        var def = paletteRepo.GetById(effectiveId);
        var colors = ColorPaletteParser.Parse(def);
        string prefix = MenuSelectionAffordance.GetPrefix(selected);
        string labelWithPrefix = prefix + labelText;
        int labelCols = DisplayWidth.GetDisplayWidth(labelWithPrefix);
        int nameMaxCols = Math.Max(0, rightColWidth - labelCols);
        string truncatedName = nameMaxCols > 0
            ? StaticTextViewport.TruncateWithEllipsis(new PlainText(namePart), nameMaxCols)
            : "";
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysis, colors?.Count ?? 0);
        string coloredName = PaletteSwatchFormatter.FormatPaletteColoredName(truncatedName, colors, phase);
        string inner = labelWithPrefix + coloredName;
        return MenuSelectionAffordance.FormatAnsiSelectableRow(selected, inner, rightColWidth, selBg, selFg);
    }

    /// <summary>Plain summary for preset default palette list rows (matches <see cref="FormatPresetDefaultPaletteSettingRow"/>).</summary>
    public static string GetPresetDefaultPaletteDisplaySummary(IPaletteRepository paletteRepo, TextLayersVisualizerSettings textLayers)
        => ResolvePresetDefaultPaletteDisplayName(paletteRepo, textLayers);

    private static string ResolvePresetDefaultPaletteDisplayName(IPaletteRepository paletteRepo, TextLayersVisualizerSettings textLayers)
    {
        string? id = textLayers.PaletteId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return "(default)";
        }

        var def = paletteRepo.GetById(id);
        var fromFile = def?.Name?.Trim();
        if (!string.IsNullOrEmpty(fromFile))
        {
            return fromFile;
        }

        foreach (var p in paletteRepo.GetAll())
        {
            if (string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return !string.IsNullOrWhiteSpace(p.Name?.Trim()) ? p.Name!.Trim() : p.Id;
            }
        }

        return id;
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
