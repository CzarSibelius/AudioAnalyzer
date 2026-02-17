using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the toolbar and visualizer to the console. Implements IVisualizationRenderer.</summary>
public sealed class VisualizationPaneLayout : IVisualizationRenderer
{
    private readonly IVisualizer? _visualizer;
    private readonly VisualizerSettings? _visualizerSettings;
    private (IReadOnlyList<PaletteColor>? Palette, string? DisplayName) _palette;

    public VisualizationPaneLayout(IDisplayDimensions displayDimensions, IEnumerable<IVisualizer> visualizers, VisualizerSettings? visualizerSettings = null)
    {
        _visualizerSettings = visualizerSettings;
        _visualizer = visualizers.FirstOrDefault(v => string.Equals(v.TechnicalName, "textlayers", StringComparison.OrdinalIgnoreCase));
    }

    public void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palette = (palette, paletteDisplayName);
    }

    private const int ToolbarLineCount = 2;
    private bool _hasRendered;
    private ScrollingTextViewportState _toolbarLine1ScrollState;
    private string? _toolbarLine1LastText;
    private ScrollingTextViewportState _toolbarLine2ScrollState;
    private string? _toolbarLine2LastText;

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
                    System.Console.WriteLine(VisualizerViewport.TruncateWithEllipsis(message, viewport.Width));
                }
            }
        }
        catch (Exception ex) { _ = ex; /* Last-resort render failure: swallow to avoid crash */ }
    }

    private string GetActivePresetName()
    {
        if (_visualizerSettings?.Presets is not { Count: > 0 })
        {
            return "Preset 1";
        }
        var active = _visualizerSettings.Presets.FirstOrDefault(p =>
            string.Equals(p.Id, _visualizerSettings.ActivePresetId, StringComparison.OrdinalIgnoreCase))
            ?? _visualizerSettings.Presets[0];
        return string.IsNullOrWhiteSpace(active.Name) ? "Preset 1" : active.Name.Trim();
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

        double db = 20 * Math.Log10(Math.Max(snapshot.Volume, 0.00001));
        string bpmDisplay = snapshot.CurrentBpm > 0 ? $" | BPM: {snapshot.CurrentBpm:F0}" : "";
        string sensitivityDisplay = $" | Beat: {snapshot.BeatSensitivity:F1} (+/-)";
        string beatIndicator = snapshot.BeatFlashActive ? " *BEAT*" : "";
        string line1Full = $"Volume: {snapshot.Volume * 100:F1}% ({db:F1} dB){bpmDisplay}{sensitivityDisplay}{beatIndicator}";
        if (line1Full != _toolbarLine1LastText)
        {
            _toolbarLine1ScrollState.Reset();
            _toolbarLine1LastText = line1Full;
        }
        string line1 = line1Full.Length > w
            ? ScrollingTextViewport.Render(line1Full, w, ref _toolbarLine1ScrollState, 0.25)
            : line1Full.PadRight(w);
        string line2;
        if (toolbarViewport.MaxLines >= 2)
        {
            string line2Full = GetToolbarLine2(snapshot, w);
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
            System.Console.SetCursorPosition(0, row0);
            System.Console.Write(snapshot.BeatFlashActive ? AnsiConsole.ToAnsiString(line1, ConsoleColor.Red) : line1);
            if (toolbarViewport.MaxLines >= 2)
            {
                System.Console.SetCursorPosition(0, row0 + 1);
                string toWrite = line2.Contains('\x1b')
                    ? line2
                    : AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray);
                System.Console.Write(toWrite);
            }
        }
        catch
        {
            try
            {
                System.Console.SetCursorPosition(0, row0);
                System.Console.WriteLine(snapshot.BeatFlashActive ? AnsiConsole.ToAnsiString(line1, ConsoleColor.Red) : line1);
                if (toolbarViewport.MaxLines >= 2)
                {
                    System.Console.SetCursorPosition(0, row0 + 1);
                    string fallbackWrite = line2.Contains('\x1b')
                        ? line2
                        : AnsiConsole.ToAnsiString(line2, ConsoleColor.DarkGray);
                    System.Console.WriteLine(fallbackWrite);
                }
            }
            catch (Exception ex) { _ = ex; /* Toolbar fallback failed: swallow to avoid crash */ }
        }
    }

    private string GetToolbarLine2(AnalysisSnapshot snapshot, int w)
    {
        string baseLine = _visualizerSettings?.Presets is { Count: > 0 }
            ? $"Preset: {GetActivePresetName()} (V)"
            : $"Mode: {_visualizer?.DisplayName ?? "Layered text"} (V)";
        if (_visualizer != null)
        {
            var suffix = _visualizer.GetToolbarSuffix(snapshot);
            if (!string.IsNullOrEmpty(suffix))
            {
                baseLine += $" | {suffix}";
            }
        }

        if (SupportsPaletteCycling() && !string.IsNullOrEmpty(snapshot.CurrentPaletteName))
        {
            baseLine += $" | Palette: {snapshot.CurrentPaletteName} (P)";
        }

        baseLine += " | H=Help";
        return baseLine;
    }
}
