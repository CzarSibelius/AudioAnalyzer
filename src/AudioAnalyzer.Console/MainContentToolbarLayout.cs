using System.Globalization;
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
        UiPalette effectiveUiPalette,
        IVisualizer visualizer,
        IReadOnlyList<PaletteColor>? paletteForSwatch,
        string? paletteDisplayName,
        int width)
    {
        (int cell1Width, int cell2Width) = GetToolbarCellWidths(width);
        var descriptors = new List<LabeledValueDescriptor>
        {
            new LabeledValueDescriptor("", () => new PlainText("General settings — Tab: mode  Up/Down: menu  Enter: open  BPM: cycle source")),
            visualizer.SupportsPaletteCycling && !string.IsNullOrEmpty(paletteDisplayName)
                ? new LabeledValueDescriptor("Palette", () =>
                {
                    var sb = new StringBuilder();
                    AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette"), effectiveUiPalette.Label);
                    var analysis = context.Frame!.Analysis;
                    int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysis, paletteForSwatch?.Count ?? 0);
                    sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteDisplayName ?? "", paletteForSwatch, phase));
                    return new AnsiText(sb.ToString());
                }, preformattedAnsi: true)
                : new LabeledValueDescriptor("", () => new PlainText(""))
        };
        return AppendFpsToolbarCellIfEnabled(uiSettings, context.Frame!, descriptors, new[] { cell1Width, cell2Width });
    }

    public static (IReadOnlyList<LabeledValueDescriptor> Descriptors, IReadOnlyList<int> Widths) BuildVisualizerToolbarRowData(
        RenderContext context,
        UiSettings uiSettings,
        UiPalette effectiveUiPalette,
        IVisualizer visualizer,
        IReadOnlyList<PaletteColor>? paletteForSwatch,
        string? paletteDisplayName,
        int width)
    {
        if (width < 30)
        {
            width = context.Width;
        }

        IReadOnlyList<LabeledValueDescriptor>? segmentDescriptors = visualizer.GetToolbarViewports(context.Frame!);
        if (segmentDescriptors is { Count: > 0 })
        {
            int[] naturals = ToolbarSegmentPackedWidths.MeasureNaturalWidths(segmentDescriptors);
            int[] segmentWidths = ToolbarSegmentSpreadWidths.GetSpreadWidths(segmentDescriptors, naturals, width);
            return AppendFpsToolbarCellIfEnabled(
                uiSettings,
                context.Frame!,
                segmentDescriptors,
                segmentWidths,
                naturalContentWidthsForFpsDonor: naturals);
        }

        (int cell1Width, int cell2Width) = GetToolbarCellWidths(width);
        string? toolbarSuffix = visualizer.GetToolbarSuffix(context.Frame!);
        var descriptors = new List<LabeledValueDescriptor>
        {
            new LabeledValueDescriptor("", () => toolbarSuffix != null ? new AnsiText(toolbarSuffix) : (IDisplayText)new PlainText(""), preformattedAnsi: true),
            visualizer.SupportsPaletteCycling && !string.IsNullOrEmpty(context.PaletteDisplayName)
                ? new LabeledValueDescriptor("Palette", () =>
                {
                    var sb = new StringBuilder();
                    AnsiConsole.AppendColored(sb, LabelFormatting.FormatLabel("Palette"), effectiveUiPalette.Label);
                    var analysis = context.Frame!.Analysis;
                    int phase = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(analysis, paletteForSwatch?.Count ?? 0);
                    sb.Append(PaletteSwatchFormatter.FormatPaletteColoredName(paletteDisplayName ?? "", paletteForSwatch, phase));
                    return new AnsiText(sb.ToString());
                }, preformattedAnsi: true)
                : new LabeledValueDescriptor("", () => new PlainText(""))
        };
        var widths = new[] { cell1Width, cell2Width };
        return AppendFpsToolbarCellIfEnabled(uiSettings, context.Frame!, descriptors, widths);
    }

    /// <summary>
    /// When <see cref="UiSettings.ShowRenderFps"/> is true, appends an FPS value column (ADR-0067).
    /// </summary>
    /// <param name="naturalContentWidthsForFpsDonor">When set (same length as <paramref name="widths"/>), a donor cell must satisfy
    /// <c>widths[i] - fpsColumnWidth &gt;= naturalContentWidthsForFpsDonor[i]</c> so content is not truncated after donating width.</param>
    public static (IReadOnlyList<LabeledValueDescriptor> Descriptors, IReadOnlyList<int> Widths) AppendFpsToolbarCellIfEnabled(
        UiSettings uiSettings,
        VisualizationFrameContext frame,
        IReadOnlyList<LabeledValueDescriptor> descriptors,
        IReadOnlyList<int> widths,
        IReadOnlyList<int>? naturalContentWidthsForFpsDonor = null)
    {
        if (!uiSettings.ShowRenderFps || descriptors.Count == 0 || widths.Count != descriptors.Count)
        {
            return (descriptors, widths);
        }

        const int fpsColumnWidth = 8;
        const int minRemainInDonor = 8;
        var w = new List<int>(widths);
        int donor = -1;
        int bestSlack = int.MinValue;
        bool useNaturalDonorRule = naturalContentWidthsForFpsDonor is { Count: var n } && n == w.Count;
        for (int i = 0; i < w.Count; i++)
        {
            int contentFloor = useNaturalDonorRule
                ? Math.Max(0, naturalContentWidthsForFpsDonor![i])
                : minRemainInDonor;
            if (w[i] < fpsColumnWidth + contentFloor)
            {
                continue;
            }

            int slackAfterDonate = w[i] - fpsColumnWidth - contentFloor;
            if (slackAfterDonate > bestSlack || (slackAfterDonate == bestSlack && i > donor))
            {
                bestSlack = slackAfterDonate;
                donor = i;
            }
        }

        if (donor < 0)
        {
            return (descriptors, widths);
        }

        w[donor] -= fpsColumnWidth;
        var d = new List<LabeledValueDescriptor>(descriptors)
        {
            new LabeledValueDescriptor("FPS", () =>
            {
                if (!frame.MeasuredMainRenderFps.HasValue)
                {
                    return new PlainText("\u2014");
                }

                int rounded = (int)Math.Round(Math.Clamp(frame.MeasuredMainRenderFps.Value, 0, 999));
                return new PlainText(rounded.ToString(CultureInfo.InvariantCulture));
            })
        };
        w.Add(fpsColumnWidth);
        return (d, w);
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
