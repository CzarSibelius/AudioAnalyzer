using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>Builds the TextLayers toolbar suffix: one digit per existing layer (1-based order), selected layer highlighted, disabled layers dimmed; optional contextual rows; palette name.</summary>
public sealed class TextLayersToolbarBuilder : ITextLayersToolbarBuilder
{
    private readonly IUiThemeResolver _uiThemeResolver;

    /// <summary>Initializes a new instance of the <see cref="TextLayersToolbarBuilder"/> class.</summary>
    public TextLayersToolbarBuilder(IUiThemeResolver uiThemeResolver)
    {
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
    }

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
            var emptyPalette = _uiThemeResolver.GetEffectiveUiPalette();
            string layersLabel = LabelFormatting.FormatLabel("Layers");
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

        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var sb = new StringBuilder();
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Layers"), palette.Label);
        AppendPresetEditorLayerDigits(sb, sortedLayers, idx, palette);
        AppendContextualRowsPlainSuffix(sb, context);
        sb.Append(" | ");
        sb.Append(LabelFormatting.FormatLabel("Palette"));
        AppendPaletteColoredName(sb, context, paletteDef, paletteName);
        return sb.ToString();
    }

    private string BuildShowPlaySuffix(TextLayersToolbarContext context)
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
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Show"), palette.Label);
        sb.Append(show);
        sb.Append(" | ");
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Entry"), palette.Label);
        sb.Append(entry);
        AppendContextualRowsPlainSuffix(sb, context);
        sb.Append(" | ");
        AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette"), palette.Label);
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
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
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
        AnsiConsole.AppendColored(layersSb, LabelFormatting.FormatLabel("Layers"), palette.Label);
        AppendPresetEditorLayerDigits(layersSb, sortedLayers, idx, palette);
        string layersAnsi = layersSb.ToString();
        list.Add(new LabeledValueDescriptor("Layers", () => new AnsiText(layersAnsi), preformattedAnsi: true));

        AddContextualRowViewports(list, context, palette.Label, palette.Normal);

        const string paletteLabel = "Palette";
        var paletteColors = ColorPaletteParser.Parse(paletteDef);
        var paletteSb = new StringBuilder();
        AnsiConsole.AppendColored(paletteSb, LabelFormatting.FormatLabel(paletteLabel), palette.Label);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Frame.Analysis, paletteColors?.Count ?? 0);
        paletteSb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, paletteColors, phase));

        list.Add(new LabeledValueDescriptor(paletteLabel, () => new AnsiText(paletteSb.ToString()), preformattedAnsi: true));
        return list;
    }

    /// <summary>Show play: compact toolbar (show name, entry index, palette) without per-layer digit row.</summary>
    private List<LabeledValueDescriptor> BuildShowPlayViewports(TextLayersToolbarContext context)
    {
        var paletteUi = _uiThemeResolver.GetEffectiveUiPalette();
        var list = new List<LabeledValueDescriptor>();
        int termW = context.Frame.TerminalWidth > 0 ? context.Frame.TerminalWidth : 80;
        int maxShow = Math.Max(12, termW / 4);
        string rawShow = string.IsNullOrWhiteSpace(context.ActiveShowName) ? "—" : context.ActiveShowName.Trim();
        string showName = new PlainText(rawShow).TruncateWithEllipsis(maxShow);

        list.Add(new LabeledValueDescriptor("Show", () => new PlainText(showName), labelColor: paletteUi.Label, textColor: paletteUi.Normal));

        int count = context.ShowEntryCount;
        int idx = count > 0 ? Math.Clamp(context.ShowEntryIndex, 0, count - 1) : 0;
        string entryText = count > 0 ? $"{idx + 1}/{count}" : "—";
        list.Add(new LabeledValueDescriptor("Entry", () => new PlainText(entryText), labelColor: paletteUi.Label, textColor: paletteUi.Normal));

        AddContextualRowViewports(list, context, paletteUi.Label, paletteUi.Normal);

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
        AnsiConsole.AppendColored(paletteSb, LabelFormatting.FormatLabel(paletteLabel), paletteUi.Label);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Frame.Analysis, paletteColors?.Count ?? 0);
        paletteSb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, paletteColors, phase));
        list.Add(new LabeledValueDescriptor(paletteLabel, () => new AnsiText(paletteSb.ToString()), preformattedAnsi: true));
        return list;
    }

    private static void AppendPresetEditorLayerDigits(
        StringBuilder sb,
        IReadOnlyList<TextLayerSettings> sortedLayers,
        int selectedLayerIndex,
        UiPalette palette)
    {
        for (int i = 0; i < sortedLayers.Count; i++)
        {
            char digit = (char)('1' + i);
            var layer = sortedLayers[i];
            PaletteColor color = i == selectedLayerIndex
                ? palette.Highlighted
                : (!layer.Enabled ? palette.Dimmed : palette.Normal);
            AnsiConsole.AppendColored(sb, digit, color);
        }
    }

    private static void AppendPaletteColoredName(StringBuilder sb, TextLayersToolbarContext context, PaletteDefinition? paletteDef, string paletteName)
    {
        var colors = ColorPaletteParser.Parse(paletteDef);
        int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(context.Frame.Analysis, colors?.Count ?? 0);
        sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteName, colors, phase));
    }

    private static void AppendContextualRowsPlainSuffix(StringBuilder sb, TextLayersToolbarContext context)
    {
        int maxW = GetContextualToolbarValueMaxWidth(context);
        foreach (var row in context.ActiveLayerContextualRows)
        {
            string v = new PlainText(row.Value).TruncateWithEllipsis(maxW);
            sb.Append(" | ");
            sb.Append(LabelFormatting.FormatLabel(row.Label));
            sb.Append(v);
        }
    }

    private static void AddContextualRowViewports(
        List<LabeledValueDescriptor> list,
        TextLayersToolbarContext context,
        PaletteColor labelColor,
        PaletteColor textColor)
    {
        int maxW = GetContextualToolbarValueMaxWidth(context);
        foreach (var row in context.ActiveLayerContextualRows)
        {
            string lbl = row.Label;
            string val = row.Value;
            list.Add(new LabeledValueDescriptor(
                lbl,
                () => new PlainText(new PlainText(val).TruncateWithEllipsis(maxW)),
                labelColor: labelColor,
                textColor: textColor));
        }
    }

    private static int GetContextualToolbarValueMaxWidth(TextLayersToolbarContext context)
    {
        int tw = context.Frame.TerminalWidth > 0 ? context.Frame.TerminalWidth : 80;
        return Math.Clamp(Math.Max(16, tw / 4), 16, 40);
    }
}
