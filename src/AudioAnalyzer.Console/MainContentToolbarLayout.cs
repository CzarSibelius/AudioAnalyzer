using System.Text;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Shared toolbar row building for main content (palette swatch, TextLayers segments, general settings hint).
/// </summary>
internal static class MainContentToolbarLayout
{
    public static (IReadOnlyList<LabeledValueDescriptor> Descriptors, IReadOnlyList<int> Widths) BuildGeneralSettingsToolbarRowData(
        RenderContext context,
        UiSettings uiSettings,
        IVisualizer visualizer,
        IReadOnlyList<PaletteColor>? paletteForSwatch,
        string? paletteDisplayName,
        int width)
    {
        (int cell1Width, int cell2Width) = GetToolbarCellWidths(width);
        var descriptors = new List<LabeledValueDescriptor>
        {
            new LabeledValueDescriptor("", () => new PlainText("General settings — Tab: mode  Up/Down: menu  Enter: open")),
            visualizer.SupportsPaletteCycling && !string.IsNullOrEmpty(paletteDisplayName)
                ? new LabeledValueDescriptor("Palette", () =>
                {
                    var pl = uiSettings.Palette ?? new UiPalette();
                    var sb = new StringBuilder();
                    AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette", null), pl.Label);
                    var snap = context.Snapshot!;
                    int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snap, paletteForSwatch?.Count ?? 0);
                    sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteDisplayName ?? "", paletteForSwatch, phase));
                    return new AnsiText(sb.ToString());
                }, preformattedAnsi: true)
                : new LabeledValueDescriptor("", () => new PlainText(""))
        };
        return (descriptors, new[] { cell1Width, cell2Width });
    }

    public static (IReadOnlyList<LabeledValueDescriptor> Descriptors, IReadOnlyList<int> Widths) BuildVisualizerToolbarRowData(
        RenderContext context,
        UiSettings uiSettings,
        IVisualizer visualizer,
        IReadOnlyList<PaletteColor>? paletteForSwatch,
        string? paletteDisplayName,
        int width)
    {
        if (width < 30)
        {
            width = context.Width;
        }

        IReadOnlyList<LabeledValueDescriptor>? segmentDescriptors = visualizer.GetToolbarViewports(context.Snapshot!);
        if (segmentDescriptors is { Count: > 0 })
        {
            IReadOnlyList<int> segmentWidths = GetToolbarSegmentWidths(width, segmentDescriptors.Count);
            return (segmentDescriptors, segmentWidths);
        }

        (int cell1Width, int cell2Width) = GetToolbarCellWidths(width);
        string? toolbarSuffix = visualizer.GetToolbarSuffix(context.Snapshot!);
        var descriptors = new List<LabeledValueDescriptor>
        {
            new LabeledValueDescriptor("", () => toolbarSuffix != null ? new AnsiText(toolbarSuffix) : (IDisplayText)new PlainText(""), preformattedAnsi: true),
            visualizer.SupportsPaletteCycling && !string.IsNullOrEmpty(context.PaletteDisplayName)
                ? new LabeledValueDescriptor("Palette", () =>
                {
                    var pl = uiSettings.Palette ?? new UiPalette();
                    var sb = new StringBuilder();
                    AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette", null), pl.Label);
                    var snap = context.Snapshot!;
                    int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snap, paletteForSwatch?.Count ?? 0);
                    sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteDisplayName ?? "", paletteForSwatch, phase));
                    return new AnsiText(sb.ToString());
                }, preformattedAnsi: true)
                : new LabeledValueDescriptor("", () => new PlainText(""))
        };
        var widths = new[] { cell1Width, cell2Width };
        return (descriptors, widths);
    }

    /// <summary>Distributes total width across N toolbar segments in 8-column blocks.</summary>
    public static int[] GetToolbarSegmentWidths(int totalWidth, int segmentCount)
    {
        if (segmentCount <= 0)
        {
            return [];
        }
        int baseBlock = Math.Max(1, totalWidth / segmentCount / 8);
        int baseWidth = baseBlock * 8;
        int[] widths = new int[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            widths[i] = baseWidth;
        }
        int remainder = totalWidth - (baseWidth * segmentCount);
        if (remainder > 0 && segmentCount > 0)
        {
            widths[segmentCount - 1] += remainder;
        }
        return widths;
    }

    public static (int Cell1Width, int Cell2Width) GetToolbarCellWidths(int width)
    {
        int cell2Width = Math.Max(16, (width / 4 / 8) * 8);
        int cell1Width = width - cell2Width;
        if (cell1Width < 8)
        {
            cell1Width = Math.Max(0, width - 16);
            cell2Width = width - cell1Width;
        }
        return (cell1Width, cell2Width);
    }
}
