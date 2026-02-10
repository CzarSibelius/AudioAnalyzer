using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Infrastructure;

public sealed class CompositeVisualizationRenderer : IVisualizationRenderer
{
    private readonly IDisplayDimensions _displayDimensions;
    private readonly Dictionary<VisualizationMode, IVisualizer> _visualizers;
    private IReadOnlyList<PaletteColor>? _palette;
    private string? _currentPaletteDisplayName;
    private TextLayersVisualizerSettings? _textLayersSettings;

    public CompositeVisualizationRenderer(IDisplayDimensions displayDimensions)
    {
        _displayDimensions = displayDimensions;
        _visualizers = new Dictionary<VisualizationMode, IVisualizer>
        {
            [VisualizationMode.SpectrumBars] = new SpectrumBarsVisualizer(),
            [VisualizationMode.Oscilloscope] = new OscilloscopeVisualizer(),
            [VisualizationMode.VuMeter] = new VuMeterVisualizer(),
            [VisualizationMode.WinampBars] = new WinampBarsVisualizer(),
            [VisualizationMode.Geiss] = new GeissVisualizer(),
            [VisualizationMode.UnknownPleasures] = new UnknownPleasuresVisualizer(),
            [VisualizationMode.TextLayers] = new TextLayersVisualizer()
        };
    }

    public void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palette = palette;
        _currentPaletteDisplayName = paletteDisplayName;
    }

    public void SetTextLayersSettings(TextLayersVisualizerSettings? settings)
    {
        _textLayersSettings = settings;
    }

    private const int ToolbarLineCount = 2;
    private VisualizationMode? _lastRenderedMode;

    public void Render(AnalysisSnapshot snapshot, VisualizationMode mode)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15)
            {
                return;
            }

            if (snapshot.DisplayStartRow < 0 || snapshot.DisplayStartRow + ToolbarLineCount >= snapshot.TerminalHeight)
            {
                return;
            }

            int termWidth = snapshot.TerminalWidth;
            var toolbarViewport = new VisualizerViewport(snapshot.DisplayStartRow, ToolbarLineCount, termWidth);
            RenderToolbar(snapshot, toolbarViewport, mode, termWidth);

            int visualizerStartRow = snapshot.DisplayStartRow + ToolbarLineCount;
            // Reserve one row at the bottom so we never write to the last buffer row and trigger scrolling
            int maxLines = Math.Max(1, snapshot.TerminalHeight - visualizerStartRow - 1);
            var viewport = new VisualizerViewport(visualizerStartRow, maxLines, termWidth);

            // Clear visualizer area only when mode changed, so we remove leftover content without flickering every frame
            if (_lastRenderedMode != mode)
            {
                ClearRegion(visualizerStartRow, maxLines, termWidth);
                _lastRenderedMode = mode;
            }

            if (_visualizers.TryGetValue(mode, out var visualizer) && visualizer.SupportsPaletteCycling)
            {
                snapshot.Palette = _palette ?? ColorPaletteParser.DefaultPalette;
                snapshot.CurrentPaletteName = _currentPaletteDisplayName;
            }

            if (mode == VisualizationMode.TextLayers)
            {
                snapshot.TextLayersConfig = _textLayersSettings;
            }

            if (_visualizers.TryGetValue(mode, out visualizer))
            {
                try
                {
                    visualizer.Render(snapshot, viewport);
                }
                catch
                {
                    Console.SetCursorPosition(0, visualizerStartRow);
                    Console.WriteLine(VisualizerViewport.TruncateToWidth("Visualization error", viewport.Width));
                }
            }
        }
        catch { }
    }

    public string GetDisplayName(VisualizationMode mode) =>
        _visualizers.TryGetValue(mode, out var v) ? v.DisplayName : "Unknown";

    public string GetTechnicalName(VisualizationMode mode) =>
        _visualizers.TryGetValue(mode, out var v) ? v.TechnicalName : "unknown";

    public bool SupportsPaletteCycling(VisualizationMode mode) =>
        _visualizers.TryGetValue(mode, out var v) && v.SupportsPaletteCycling;

    public VisualizationMode? GetModeFromTechnicalName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        key = key.Trim();
        foreach (var (mode, visualizer) in _visualizers)
        {
            if (string.Equals(visualizer.TechnicalName, key, StringComparison.OrdinalIgnoreCase))
            {
                return mode;
            }
        }
        return null;
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
                Console.SetCursorPosition(0, startRow + i);
                Console.Write(blank);
            }
        }
        catch { }
    }

    private void RenderToolbar(AnalysisSnapshot snapshot, VisualizerViewport toolbarViewport, VisualizationMode mode, int w)
    {
        int row0 = toolbarViewport.StartRow;

        double db = 20 * Math.Log10(Math.Max(snapshot.Volume, 0.00001));
        string bpmDisplay = snapshot.CurrentBpm > 0 ? $" | BPM: {snapshot.CurrentBpm:F0}" : "";
        string sensitivityDisplay = $" | Beat: {snapshot.BeatSensitivity:F1} (+/-)";
        string beatIndicator = snapshot.BeatFlashActive ? " *BEAT*" : "";
        string line1 = VisualizerViewport.TruncateToWidth(
            $"Volume: {snapshot.Volume * 100:F1}% ({db:F1} dB){bpmDisplay}{sensitivityDisplay}{beatIndicator}",
            w).PadRight(w);
        string line2 = toolbarViewport.MaxLines >= 2
            ? VisualizerViewport.TruncateToWidth(GetToolbarLine2(mode, snapshot, w), w).PadRight(w)
            : new string(' ', w);

        try
        {
            Console.SetCursorPosition(0, row0);
            Console.Write(snapshot.BeatFlashActive ? AnsiConsole.ToAnsiString(line1, ConsoleColor.Red) : line1);
            if (toolbarViewport.MaxLines >= 2)
            {
                Console.SetCursorPosition(0, row0 + 1);
                Console.Write(AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray));
            }
        }
        catch
        {
            try
            {
                Console.SetCursorPosition(0, row0);
                Console.WriteLine(snapshot.BeatFlashActive ? AnsiConsole.ToAnsiString(line1, ConsoleColor.Red) : line1);
                if (toolbarViewport.MaxLines >= 2)
                {
                    Console.SetCursorPosition(0, row0 + 1);
                    Console.WriteLine(AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray));
                }
            }
            catch { }
        }
    }

    private string GetToolbarLine2(VisualizationMode mode, AnalysisSnapshot snapshot, int w)
    {
        string displayName = GetDisplayName(mode);
        string baseLine = $"Mode: {displayName} (V)";
        if (_visualizers.TryGetValue(mode, out var visualizer))
        {
            var suffix = visualizer.GetToolbarSuffix(snapshot);
            if (!string.IsNullOrEmpty(suffix))
            {
                baseLine += $" | {suffix}";
            }
        }

        if (SupportsPaletteCycling(mode) && !string.IsNullOrEmpty(snapshot.CurrentPaletteName))
        {
            baseLine += $" | Palette: {snapshot.CurrentPaletteName} (P)";
        }

        baseLine += " | H=Help";
        return baseLine;
    }
}