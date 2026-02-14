using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class VisualizationPaneLayout : IVisualizationRenderer
{
    private static readonly Dictionary<string, VisualizationMode> s_technicalNameToMode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spectrum"] = VisualizationMode.SpectrumBars,
        ["vumeter"] = VisualizationMode.VuMeter,
        ["winamp"] = VisualizationMode.WinampBars,
        ["textlayers"] = VisualizationMode.TextLayers,
    };

    private readonly IDisplayDimensions _displayDimensions;
    private readonly Dictionary<VisualizationMode, IVisualizer> _visualizers;
    private readonly Dictionary<VisualizationMode, (IReadOnlyList<PaletteColor>? Palette, string? DisplayName)> _palettes = new();

    public VisualizationPaneLayout(IDisplayDimensions displayDimensions, IEnumerable<IVisualizer> visualizers)
    {
        _displayDimensions = displayDimensions;
        _visualizers = new Dictionary<VisualizationMode, IVisualizer>();
        foreach (var v in visualizers)
        {
            if (s_technicalNameToMode.TryGetValue(v.TechnicalName, out var mode))
            {
                _visualizers[mode] = v;
            }
        }
    }

    public void SetPaletteForMode(VisualizationMode mode, IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palettes[mode] = (palette, paletteDisplayName);
    }

    private const int ToolbarLineCount = 2;
    private VisualizationMode? _lastRenderedMode;
    private ScrollingTextViewportState _toolbarLine2ScrollState;
    private string? _toolbarLine2LastText;

    public void Render(AnalysisSnapshot snapshot, VisualizationMode mode)
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
                RenderToolbar(snapshot, toolbarViewport, mode, termWidth);

                visualizerStartRow = snapshot.DisplayStartRow + ToolbarLineCount;
                maxLines = Math.Max(1, snapshot.TerminalHeight - visualizerStartRow - 1);
            }

            var viewport = new VisualizerViewport(visualizerStartRow, maxLines, termWidth);

            // Clear visualizer area only when mode changed, so we remove leftover content without flickering every frame
            if (_lastRenderedMode != mode)
            {
                ClearRegion(visualizerStartRow, maxLines, termWidth);
                _lastRenderedMode = mode;
            }

            if (_visualizers.TryGetValue(mode, out var visualizer) && visualizer.SupportsPaletteCycling
                && mode != VisualizationMode.TextLayers)
            {
                var (palette, displayName) = _palettes.TryGetValue(mode, out var entry) ? entry : (default(IReadOnlyList<PaletteColor>?), null);
                snapshot.Palette = palette ?? ColorPaletteParser.DefaultPalette;
                snapshot.CurrentPaletteName = displayName;
            }

            if (_visualizers.TryGetValue(mode, out visualizer))
            {
                try
                {
                    visualizer.Render(snapshot, viewport);
                }
                catch (Exception ex)
                {
                    string message = !string.IsNullOrWhiteSpace(ex.Message)
                        ? ex.Message
                        : "Visualization error";
                    Console.SetCursorPosition(0, visualizerStartRow);
                    Console.WriteLine(VisualizerViewport.TruncateToWidth(message, viewport.Width));
                }
            }
        }
        catch (Exception ex) { _ = ex; /* Last-resort render failure: swallow to avoid crash */ }
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

    public bool HandleKey(ConsoleKeyInfo key, VisualizationMode mode)
    {
        if (_visualizers.TryGetValue(mode, out var visualizer))
        {
            return visualizer.HandleKey(key);
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
                Console.SetCursorPosition(0, startRow + i);
                Console.Write(blank);
            }
        }
        catch (Exception ex) { _ = ex; /* Console write failed in ClearRegion */ }
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
        string line2;
        if (toolbarViewport.MaxLines >= 2)
        {
            string line2Full = GetToolbarLine2(mode, snapshot, w);
            int visibleLen = AnsiConsole.GetVisibleLength(line2Full);
            if (line2Full != _toolbarLine2LastText)
            {
                _toolbarLine2ScrollState.Reset();
                _toolbarLine2LastText = line2Full;
            }
            line2 = visibleLen > w
                ? ScrollingTextViewport.RenderWithAnsi(line2Full, w, ref _toolbarLine2ScrollState, 0.25)
                : AnsiConsole.PadToVisibleWidth(line2Full, w);
            _toolbarLine2LastText = line2Full;
        }
        else
        {
            line2 = new string(' ', w);
        }

        try
        {
            Console.SetCursorPosition(0, row0);
            Console.Write(snapshot.BeatFlashActive ? AnsiConsole.ToAnsiString(line1, ConsoleColor.Red) : line1);
            if (toolbarViewport.MaxLines >= 2)
            {
                Console.SetCursorPosition(0, row0 + 1);
                string toWrite = line2.Contains('\x1b')
                    ? line2
                    : AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray);
                Console.Write(toWrite);
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
                    string fallbackWrite = line2.Contains('\x1b')
                        ? line2
                        : AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray);
                    Console.WriteLine(fallbackWrite);
                }
            }
            catch (Exception ex) { _ = ex; /* Toolbar fallback failed: swallow to avoid crash */ }
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
