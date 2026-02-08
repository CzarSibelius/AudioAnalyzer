using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;

namespace AudioAnalyzer.Infrastructure;

public sealed class CompositeVisualizationRenderer : IVisualizationRenderer
{
    private readonly IDisplayDimensions _displayDimensions;
    private readonly Dictionary<VisualizationMode, IVisualizer> _visualizers;
    private readonly GeissVisualizer _geissVisualizer;

    public CompositeVisualizationRenderer(IDisplayDimensions displayDimensions)
    {
        _displayDimensions = displayDimensions;
        _geissVisualizer = new GeissVisualizer();
        _visualizers = new Dictionary<VisualizationMode, IVisualizer>
        {
            [VisualizationMode.SpectrumBars] = new SpectrumBarsVisualizer(),
            [VisualizationMode.Oscilloscope] = new OscilloscopeVisualizer(),
            [VisualizationMode.VuMeter] = new VuMeterVisualizer(),
            [VisualizationMode.WinampBars] = new WinampBarsVisualizer(),
            [VisualizationMode.Geiss] = _geissVisualizer
        };
    }

    private const int ToolbarLineCount = 2;
    private VisualizationMode? _lastRenderedMode;

    public void Render(AnalysisSnapshot snapshot, VisualizationMode mode)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15) return;
            if (snapshot.DisplayStartRow < 0 || snapshot.DisplayStartRow + ToolbarLineCount >= snapshot.TerminalHeight) return;

            int termWidth = snapshot.TerminalWidth;
            var toolbarViewport = new VisualizerViewport(snapshot.DisplayStartRow, ToolbarLineCount, termWidth);
            RenderToolbar(snapshot, toolbarViewport, mode);

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

            if (_visualizers.TryGetValue(mode, out var visualizer))
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

    public void SetShowBeatCircles(bool show)
    {
        _geissVisualizer.ShowBeatCircles = show;
    }

    public bool GetShowBeatCircles()
    {
        return _geissVisualizer.ShowBeatCircles;
    }

    private static void ClearRegion(int startRow, int lineCount, int width)
    {
        if (width <= 0 || lineCount <= 0) return;
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

    private static void RenderToolbar(AnalysisSnapshot snapshot, VisualizerViewport toolbarViewport, VisualizationMode mode)
    {
        int w = toolbarViewport.Width;
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

    private static string GetToolbarLine2(VisualizationMode mode, AnalysisSnapshot snapshot, int w)
    {
        string baseLine = $"Mode: {GetModeName(mode)} (V) | H=Help";
        if (mode == VisualizationMode.Oscilloscope)
            baseLine = $"Mode: {GetModeName(mode)} (V) | Gain: {snapshot.OscilloscopeGain:F1} ([ ]) | H=Help";
        return baseLine;
    }

    private static string GetModeName(VisualizationMode mode)
    {
        return mode switch
        {
            VisualizationMode.SpectrumBars => "Spectrum Analyzer",
            VisualizationMode.Oscilloscope => "Oscilloscope",
            VisualizationMode.VuMeter => "VU Meter",
            VisualizationMode.WinampBars => "Winamp Style",
            VisualizationMode.Geiss => "Geiss",
            _ => "Unknown"
        };
    }
}