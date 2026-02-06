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

    public void Render(AnalysisSnapshot snapshot, VisualizationMode mode)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15) return;
            Console.SetCursorPosition(0, snapshot.DisplayStartRow);
            int termWidth = snapshot.TerminalWidth;
            DisplayVolumeInfo(snapshot, termWidth, mode);
            if (_visualizers.TryGetValue(mode, out var visualizer))
                visualizer.Render(snapshot, _displayDimensions, snapshot.DisplayStartRow + 2);
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

    private static void DisplayVolumeInfo(AnalysisSnapshot snapshot, int termWidth, VisualizationMode mode)
    {
        double db = 20 * Math.Log10(Math.Max(snapshot.Volume, 0.00001));
        string bpmDisplay = snapshot.CurrentBpm > 0 ? $" | BPM: {snapshot.CurrentBpm:F0}" : "";
        string sensitivityDisplay = $" | Beat: {snapshot.BeatSensitivity:F1} (+/-)";
        string beatIndicator = snapshot.BeatFlashActive ? " *BEAT*" : "";
        if (snapshot.BeatFlashActive) Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Volume: {snapshot.Volume * 100:F1}% ({db:F1} dB){bpmDisplay}{sensitivityDisplay}{beatIndicator}".PadRight(termWidth));
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        string modeName = GetModeName(mode);
        Console.WriteLine($"Mode: {modeName} (V) | S=Save | H=Help".PadRight(termWidth));
        Console.ResetColor();
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