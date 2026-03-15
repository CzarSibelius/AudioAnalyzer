using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Main content region: toolbar row + visualizer area. Implements <see cref="IVisualizationRenderer"/>
/// so the orchestrator and shell keep the same contract. Builds a root <see cref="CompositeComponent"/> and renders via <see cref="IUiComponentRenderer{TComponent}"/>.
/// </summary>
internal sealed class MainContentContainer : IVisualizationRenderer
{
    private const int ToolbarLineCount = 1;

    private readonly IVisualizer _visualizer;
    private readonly IDisplayState _displayState;
    private readonly UiSettings _uiSettings;
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly IUiStateUpdater<IUiComponent> _stateUpdater;
    private readonly HorizontalRowComponent _toolbarRow;

    private (IReadOnlyList<PaletteColor>? Palette, string? DisplayName) _palette;

    public MainContentContainer(
        IUiComponentRenderer<IUiComponent> componentRenderer,
        IUiStateUpdater<IUiComponent> stateUpdater,
        IVisualizer visualizer,
        IDisplayState displayState,
        UiSettings uiSettings)
    {
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _stateUpdater = stateUpdater ?? throw new ArgumentNullException(nameof(stateUpdater));
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _uiSettings = uiSettings ?? new UiSettings();

        _displayState.Changed += (_, _) =>
        {
            if (!_displayState.FullScreen)
            {
                _componentRenderer.ResetVisualizerAreaCleared();
            }
        };

        _toolbarRow = new HorizontalRowComponent();
    }

    /// <inheritdoc />
    public void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palette = (palette, paletteDisplayName);
    }

    /// <inheritdoc />
    public bool SupportsPaletteCycling() =>
        _visualizer is { SupportsPaletteCycling: true };

    /// <inheritdoc />
    public bool HandleKey(ConsoleKeyInfo key)
    {
        return _visualizer.HandleKey(key);
    }

    /// <inheritdoc />
    public void Render(AnalysisSnapshot snapshot)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15)
            {
                return;
            }

            int termWidth = snapshot.TerminalWidth;
            int startRow = snapshot.DisplayStartRow;

            if (_displayState.FullScreen)
            {
                startRow = 0;
            }
            else
            {
                if (startRow < 0 || startRow + ToolbarLineCount >= snapshot.TerminalHeight)
                {
                    return;
                }
            }

            int visualizerMaxLines = _displayState.FullScreen
                ? Math.Max(1, snapshot.TerminalHeight - 1)
                : Math.Max(1, snapshot.TerminalHeight - startRow - ToolbarLineCount - 1);

            var palette = _uiSettings.Palette ?? new UiPalette();
            var context = new RenderContext
            {
                Width = termWidth,
                StartRow = startRow,
                MaxLines = visualizerMaxLines,
                Palette = palette,
                ScrollSpeed = _uiSettings.DefaultScrollingSpeed,
                Snapshot = snapshot,
                PaletteDisplayName = _palette.DisplayName
            };

            var root = new CompositeComponent(GetMainComponents);
            _stateUpdater.Update(root, context);
            _componentRenderer.Render(root, context);
        }
        catch (Exception ex)
        {
            _ = ex; /* Last-resort render failure: swallow to avoid crash */
        }
    }

    private IReadOnlyList<IUiComponent> GetMainComponents(RenderContext context)
    {
        if (_displayState.FullScreen)
        {
            return [VisualizerAreaComponent.Instance];
        }
        (IReadOnlyList<LabeledValueDescriptor> descriptors, IReadOnlyList<int> widths) = BuildToolbarRowData(context);
        _toolbarRow.SetRowData(descriptors, widths);
        return [_toolbarRow, VisualizerAreaComponent.Instance];
    }

    private (IReadOnlyList<LabeledValueDescriptor> Descriptors, IReadOnlyList<int> Widths) BuildToolbarRowData(RenderContext context)
    {
        int width = context.Snapshot?.TerminalWidth ?? context.Width;
        if (width < 30)
        {
            width = context.Width;
        }

        IReadOnlyList<LabeledValueDescriptor>? segmentDescriptors = _visualizer.GetToolbarViewports(context.Snapshot!);
        if (segmentDescriptors is { Count: > 0 })
        {
            IReadOnlyList<int> segmentWidths = GetToolbarSegmentWidths(width, segmentDescriptors.Count);
            return (segmentDescriptors, segmentWidths);
        }

        (int cell1Width, int cell2Width) = GetToolbarCellWidths(width);
        string? toolbarSuffix = _visualizer.GetToolbarSuffix(context.Snapshot!);
        var descriptors = new List<LabeledValueDescriptor>
        {
            new LabeledValueDescriptor("", () => toolbarSuffix != null ? new AnsiText(toolbarSuffix) : (IDisplayText)new PlainText(""), preformattedAnsi: true),
            _visualizer.SupportsPaletteCycling && !string.IsNullOrEmpty(context.PaletteDisplayName)
                ? new LabeledValueDescriptor("Palette", () => new PlainText(context.PaletteDisplayName ?? ""), labelColor: (_uiSettings.Palette ?? new UiPalette()).Label, textColor: (_uiSettings.Palette ?? new UiPalette()).Normal)
                : new LabeledValueDescriptor("", () => new PlainText(""))
        };
        var widths = new[] { cell1Width, cell2Width };
        return (descriptors, widths);
    }

    /// <summary>Distributes total width across N toolbar segments in 8-column blocks.</summary>
    private static int[] GetToolbarSegmentWidths(int totalWidth, int segmentCount)
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

    private static (int Cell1Width, int Cell2Width) GetToolbarCellWidths(int width)
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
