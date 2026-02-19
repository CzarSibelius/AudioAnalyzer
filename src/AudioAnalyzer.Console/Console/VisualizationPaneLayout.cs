using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the toolbar and visualizer to the console. Implements IVisualizationRenderer.</summary>
public sealed class VisualizationPaneLayout : IVisualizationRenderer
{
    private readonly IVisualizer? _visualizer;
    private readonly VisualizerSettings? _visualizerSettings;
    private readonly UiSettings _uiSettings;
    private readonly IScrollingTextViewport _labelViewport;
    private (IReadOnlyList<PaletteColor>? Palette, string? DisplayName) _palette;

    public VisualizationPaneLayout(IDisplayDimensions displayDimensions, IEnumerable<IVisualizer> visualizers, VisualizerSettings? visualizerSettings, UiSettings? uiSettings, IScrollingTextViewportFactory viewportFactory)
    {
        _visualizerSettings = visualizerSettings;
        _visualizer = visualizers.FirstOrDefault(v => string.Equals(v.TechnicalName, "textlayers", StringComparison.OrdinalIgnoreCase));
        _uiSettings = uiSettings ?? new UiSettings();
        _labelViewport = viewportFactory.CreateViewport();
    }

    public void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palette = (palette, paletteDisplayName);
    }

    private const int ToolbarLineCount = 1;
    private bool _hasRendered;

    public void Render(AnalysisSnapshot snapshot)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15)
            {
                return;
            }

            int termWidth = snapshot.TerminalWidth;
            int visualizerStartRow;
            int maxLines;

            if (snapshot.FullScreenMode)
            {
                visualizerStartRow = 0;
                maxLines = Math.Max(1, snapshot.TerminalHeight - 1);
            }
            else
            {
                if (snapshot.DisplayStartRow < 0 || snapshot.DisplayStartRow + ToolbarLineCount >= snapshot.TerminalHeight)
                {
                    return;
                }

                var toolbarViewport = new VisualizerViewport(snapshot.DisplayStartRow, ToolbarLineCount, termWidth);
                RenderToolbar(snapshot, toolbarViewport, termWidth);

                visualizerStartRow = snapshot.DisplayStartRow + ToolbarLineCount;
                maxLines = Math.Max(1, snapshot.TerminalHeight - visualizerStartRow - 1);
            }

            var viewport = new VisualizerViewport(visualizerStartRow, maxLines, termWidth);

            if (!_hasRendered)
            {
                ClearRegion(visualizerStartRow, maxLines, termWidth);
                _hasRendered = true;
            }

            if (_visualizer is { SupportsPaletteCycling: true })
            {
                snapshot.CurrentPaletteName = _palette.DisplayName;
            }

            if (_visualizer != null)
            {
                try
                {
                    _visualizer.Render(snapshot, viewport);
                }
                catch (Exception ex)
                {
                    string message = !string.IsNullOrWhiteSpace(ex.Message)
                        ? ex.Message
                        : "Visualization error";
                    System.Console.SetCursorPosition(0, visualizerStartRow);
                    System.Console.WriteLine(StaticTextViewport.TruncateWithEllipsis(new PlainText(message), viewport.Width));
                }
            }
        }
        catch (Exception ex) { _ = ex; /* Last-resort render failure: swallow to avoid crash */ }
    }

    public bool SupportsPaletteCycling() =>
        _visualizer is { SupportsPaletteCycling: true };

    public bool HandleKey(ConsoleKeyInfo key)
    {
        if (_visualizer != null)
        {
            return _visualizer.HandleKey(key);
        }
        return false;
    }

    private static void ClearRegion(int startRow, int lineCount, int width)
    {
        if (width <= 0 || lineCount <= 0)
        {
            return;
        }

        string blank = new string(' ', width);
        try
        {
            for (int i = 0; i < lineCount; i++)
            {
                System.Console.SetCursorPosition(0, startRow + i);
                System.Console.Write(blank);
            }
        }
        catch (Exception ex) { _ = ex; /* Console write failed in ClearRegion */ }
    }

    private void RenderToolbar(AnalysisSnapshot snapshot, VisualizerViewport toolbarViewport, int w)
    {
        int row0 = toolbarViewport.StartRow;
        if (toolbarViewport.MaxLines < 1)
        {
            return;
        }

        var palette = _uiSettings.Palette ?? new UiPalette();
        var labelColor = palette.Label;
        var dimmedColor = palette.Dimmed;

        // Split into viewports: Suffix (layers) | Palette | Help
        int cell1Width = (int)(w * 0.60);
        int cell2Width = (int)(w * 0.22);
        int cell3Width = w - cell1Width - cell2Width;
        if (cell3Width < 8)
        {
            cell3Width = 8;
            cell1Width = Math.Max(8, w - cell2Width - cell3Width);
        }

        string suffixCell = GetSuffixCell(snapshot, cell1Width);
        string paletteCell = GetPaletteCell(snapshot, labelColor, palette.Normal, cell2Width);
        string helpCell = AnsiConsole.ColorCode(dimmedColor) + "H=Help" + AnsiConsole.ResetCode;
        helpCell = AnsiConsole.PadToVisibleWidth(helpCell, cell3Width);

        string line = suffixCell + paletteCell + helpCell;
        int visible = AnsiConsole.GetVisibleLength(line);
        if (visible < w)
        {
            line = AnsiConsole.PadToVisibleWidth(line, w);
        }
        else if (visible > w)
        {
            line = AnsiConsole.GetVisibleSubstring(line, 0, w);
        }

        try
        {
            System.Console.SetCursorPosition(0, row0);
            System.Console.Write(line);
        }
        catch (Exception ex) { _ = ex; /* Toolbar write failed: swallow to avoid crash */ }
    }

    private string GetSuffixCell(AnalysisSnapshot snapshot, int width)
    {
        string suffix = _visualizer?.GetToolbarSuffix(snapshot) ?? "";
        if (string.IsNullOrEmpty(suffix))
        {
            return new string(' ', width);
        }
        string cell = AnsiConsole.PadToVisibleWidth(
            StaticTextViewport.TruncateWithEllipsis(new AnsiText(suffix), width), width);
        return cell;
    }

    private string GetPaletteCell(AnalysisSnapshot snapshot, PaletteColor labelColor, PaletteColor normalColor, int width)
    {
        if (!SupportsPaletteCycling() || string.IsNullOrEmpty(snapshot.CurrentPaletteName))
        {
            return new string(' ', width);
        }
        string label = _labelViewport.FormatLabel("Palette", "P");
        string value = $"{AnsiConsole.ColorCode(labelColor)}{label}{AnsiConsole.ResetCode}{AnsiConsole.ColorCode(normalColor)}{snapshot.CurrentPaletteName}{AnsiConsole.ResetCode}";
        string cell = AnsiConsole.PadToVisibleWidth(
            StaticTextViewport.TruncateWithEllipsis(new AnsiText(value), width), width);
        return cell;
    }
}
