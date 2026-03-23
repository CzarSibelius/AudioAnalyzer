using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>Builds the TextLayers toolbar suffix: layer digits 1–<see cref="TextLayersLimits.MaxLayerCount"/>, oscilloscope gain (when applicable), palette name.</summary>
public sealed class TextLayersToolbarBuilder : ITextLayersToolbarBuilder
{
    /// <inheritdoc />
    public string? BuildSuffix(TextLayersToolbarContext context)
    {
        if (context.ApplicationMode == ApplicationMode.ShowPlay)
        {
            return BuildShowPlaySuffix(context);
        }

        var sortedLayers = context.SortedLayers;
        if (sortedLayers is not { Count: > 0 })
        {
            var emptyPalette = context.UiSettings.Palette ?? new UiPalette();
            string layersLabel = LabelFormatting.FormatLabel("Layers", null);
            return AnsiConsole.ColorCode(emptyPalette.Label) + layersLabel + AnsiConsole.ResetCode + AnsiConsole.ColorCode(emptyPalette.Dimmed) + "(config in settings)" + AnsiConsole.ResetCode;
        }

        var config = context.Settings;
        int idx = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        var paletteId = layer.PaletteId ?? config?.PaletteId ?? "default";
        var paletteDef = context.PaletteRepo.GetById(paletteId);
        var paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        var palette = context.UiSettings.Palette ?? new UiPalette();
        var sb = new StringBuilder();
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Layers", null), palette.Label);
        for (int i = 0; i < TextLayersLimits.MaxLayerCount; i++)
        {
            char digit = (char)('1' + i);
            if (i >= sortedLayers.Count)
            {
                AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
            }
            else
            {
                var l = sortedLayers[i];
                if (!l.Enabled)
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Dimmed);
                }
                else
                {
                    AnsiConsole.AppendColored(sb, digit, palette.Normal);
                }
            }
        }
        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            double gain = context.OscilloscopeGain ?? 2.5;
            sb.Append(" | ");
            sb.Append(LabelFormatting.FormatLabel("Gain", null));
            sb.Append(gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
        }
        sb.Append(" | ");
        sb.Append(LabelFormatting.FormatLabel("Palette", null));
        AppendPaletteColoredName(sb, context, paletteDef, paletteName);
        return sb.ToString();
    }

    private static string BuildShowPlaySuffix(TextLayersToolbarContext context)
    {
        string show = string.IsNullOrWhiteSpace(context.ActiveShowName) ? "—" : context.ActiveShowName.Trim();
        int count = context.ShowEntryCount;
        int idx = count > 0 ? Math.Clamp(context.ShowEntryIndex, 0, count - 1) : 0;
        string entry = count > 0 ? $"{idx + 1}/{count}" : "—";
        var sortedLayers = context.SortedLayers;
        TextLayersVisualizerSettings? config = context.Settings;
        TextLayerSettings? layer = sortedLayers is { Count: > 0 }
            ? sortedLayers[Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1)]
            : null;
        string paletteId = layer?.PaletteId ?? config?.PaletteId ?? "default";
        var paletteDef = context.PaletteRepo.GetById(paletteId);
        string paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        var sb = new StringBuilder();
        var palette = context.UiSettings.Palette ?? new UiPalette();
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Show", null), palette.Label);
        sb.Append(show);
        sb.Append(" | ");
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Entry", null), palette.Label);
        sb.Append(entry);
        sb.Append(" | ");
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette", null), palette.Label);
        AppendPaletteColoredName(sb, context, paletteDef, paletteName);
        return sb.ToString();
    }

    /// <inheritdoc />
    public IReadOnlyList<LabeledValueDescriptor> BuildViewports(TextLayersToolbarContext context)
    {
        if (context.ApplicationMode == ApplicationMode.ShowPlay)
        {
            return BuildShowPlayViewports(context);
        }

        var sortedLayers = context.SortedLayers;
        var palette = context.UiSettings.Palette ?? new UiPalette();
        var list = new List<LabeledValueDescriptor>();

        if (sortedLayers is not { Count: > 0 })
        {
            list.Add(new LabeledValueDescriptor("Layers", () => new PlainText("(config in settings)"), labelColor: palette.Label, textColor: palette.Dimmed));
            return list;
        }

        var config = context.Settings;
        int idx = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
        var layer = sortedLayers[idx];
        var paletteId = layer.PaletteId ?? config?.PaletteId ?? "default";
        var paletteDef = context.PaletteRepo.GetById(paletteId);
        var paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        var layersSb = new StringBuilder();
        AnsiConsole.AppendColored(layersSb, LabelFormatting.FormatLabel("Layers", null), palette.Label);
        for (int i = 0; i < TextLayersLimits.MaxLayerCount; i++)
        {
            char digit = (char)('1' + i);
            if (i >= sortedLayers.Count)
            {
                AnsiConsole.AppendColored(layersSb, digit, palette.Dimmed);
            }
            else
            {
                var l = sortedLayers[i];
                if (!l.Enabled)
                {
                    AnsiConsole.AppendColored(layersSb, digit, palette.Dimmed);
                }
                else
                {
                    AnsiConsole.AppendColored(layersSb, digit, palette.Normal);
                }
            }
        }
        string layersAnsi = layersSb.ToString();
        list.Add(new LabeledValueDescriptor("Layers", () => new AnsiText(layersAnsi), preformattedAnsi: true));

        if (layer.LayerType == TextLayerType.Oscilloscope)
        {
            double gain = context.OscilloscopeGain ?? 2.5;
            string gainStr = gain.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            list.Add(new LabeledValueDescriptor("Gain", () => new PlainText(gainStr), labelColor: palette.Label, textColor: palette.Normal));
        }

        const string paletteLabel = "Palette";
        var paletteColors = ColorPaletteParser.Parse(paletteDef);
        var paletteSb = new StringBuilder();
        AnsiConsole.AppendColored(paletteSb, LabelFormatting.FormatLabel(paletteLabel, null), palette.Label);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Snapshot, paletteColors?.Count ?? 0);
        paletteSb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, paletteColors, phase));

        list.Add(new LabeledValueDescriptor(paletteLabel, () => new AnsiText(paletteSb.ToString()), preformattedAnsi: true));
        return list;
    }

    /// <summary>Show play: compact toolbar (show name, entry index, palette) without per-layer digit row.</summary>
    private static List<LabeledValueDescriptor> BuildShowPlayViewports(TextLayersToolbarContext context)
    {
        var paletteUi = context.UiSettings.Palette ?? new UiPalette();
        var list = new List<LabeledValueDescriptor>();
        int termW = context.Snapshot?.TerminalWidth > 0 ? context.Snapshot.TerminalWidth : 80;
        int maxShow = Math.Max(12, termW / 4);
        string rawShow = string.IsNullOrWhiteSpace(context.ActiveShowName) ? "—" : context.ActiveShowName.Trim();
        string showName = new PlainText(rawShow).TruncateWithEllipsis(maxShow);

        list.Add(new LabeledValueDescriptor("Show", () => new PlainText(showName), labelColor: paletteUi.Label, textColor: paletteUi.Normal));

        int count = context.ShowEntryCount;
        int idx = count > 0 ? Math.Clamp(context.ShowEntryIndex, 0, count - 1) : 0;
        string entryText = count > 0 ? $"{idx + 1}/{count}" : "—";
        list.Add(new LabeledValueDescriptor("Entry", () => new PlainText(entryText), labelColor: paletteUi.Label, textColor: paletteUi.Normal));

        TextLayerSettings? layer;
        TextLayersVisualizerSettings? config = context.Settings;
        var sortedLayers = context.SortedLayers;
        if (sortedLayers is { Count: > 0 })
        {
            int li = Math.Clamp(context.PaletteCycleLayerIndex, 0, sortedLayers.Count - 1);
            layer = sortedLayers[li];
        }
        else
        {
            layer = null;
        }

        string paletteId = layer?.PaletteId ?? config?.PaletteId ?? "default";
        var paletteDef = context.PaletteRepo.GetById(paletteId);
        string paletteName = paletteDef?.Name?.Trim() ?? paletteId;
        if (string.IsNullOrWhiteSpace(paletteName))
        {
            paletteName = "Default";
        }

        const string paletteLabel = "Palette";
        var paletteColors = ColorPaletteParser.Parse(paletteDef);
        var paletteSb = new StringBuilder();
        AnsiConsole.AppendColored(paletteSb, LabelFormatting.FormatLabel(paletteLabel, null), paletteUi.Label);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Snapshot!, paletteColors?.Count ?? 0);
        paletteSb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, paletteColors, phase));
        list.Add(new LabeledValueDescriptor(paletteLabel, () => new AnsiText(paletteSb.ToString()), preformattedAnsi: true));
        return list;
    }

    private static void AppendPaletteColoredName(StringBuilder sb, TextLayersToolbarContext context, PaletteDefinition? paletteDef, string paletteName)
    {
        var colors = ColorPaletteParser.Parse(paletteDef);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Snapshot!, colors?.Count ?? 0);
        sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, colors, phase));
    }
}
