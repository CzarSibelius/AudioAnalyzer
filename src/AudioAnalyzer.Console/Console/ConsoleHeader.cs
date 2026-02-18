using System.IO;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Renders the application header (title, device name, shortcuts) to the console.</summary>
internal static class ConsoleHeader
{
    private static ScrollingTextViewportState _deviceScrollState;
    private static string? _deviceLastText;
    private static ScrollingTextViewportState _nowPlayingScrollState;
    private static string? _nowPlayingLastText;
    private static string? _lastWrittenLine1;
    private static string? _lastWrittenLine2;
    private static string? _lastWrittenLine3;
    private static string? _lastWrittenLine4;
    private static string? _lastWrittenLine5;
    private static string? _lastWrittenLine6;

    /// <summary>Gets the current console width, or 80 if unavailable.</summary>
    public static int GetConsoleWidth()
    {
        try { return System.Console.WindowWidth; }
        catch (IOException) { return 80; }
    }

    /// <summary>Clears the console and draws the full header including device info, now-playing, and mode.</summary>
    public static void DrawMain(string deviceName, string? nowPlayingText = null, string? modeName = null)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                int w = System.Console.WindowWidth;
                int h = Math.Max(15, System.Console.WindowHeight);
                if (w >= 10 && h >= 15)
                {
                    System.Console.BufferWidth = w;
                    System.Console.BufferHeight = h;
                }
            }
        }
        catch (Exception ex) { _ = ex; /* Buffer size not supported: swallow to avoid crash */ }

        System.Console.Clear();
        System.Console.CursorVisible = false;
        InvalidateHeaderCache();
        DrawHeaderOnly(deviceName, nowPlayingText, modeName);
    }

    /// <summary>Invalidates the header cache so the next DrawHeaderOnly writes all lines. Call after Console.Clear.</summary>
    private static void InvalidateHeaderCache()
    {
        _lastWrittenLine1 = null;
        _lastWrittenLine2 = null;
        _lastWrittenLine3 = null;
        _lastWrittenLine4 = null;
        _lastWrittenLine5 = null;
        _lastWrittenLine6 = null;
    }

    /// <summary>Draws only the header lines (no clear). Used for refresh before each render.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    /// <param name="nowPlayingText">Optional now-playing text from system media session (e.g. "Artist - Title").</param>
    /// <param name="modeName">Optional mode name (e.g. "Preset editor" or "Show play").</param>
    public static void DrawHeaderOnly(string deviceName, string? nowPlayingText = null, string? modeName = null)
    {
        int width = Math.Max(10, GetConsoleWidth());
        string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
        title = VisualizerViewport.TruncateWithEllipsis(title, width - 2);
        int padding = Math.Max(0, (width - title.Length - 2) / 2);
        string line1 = VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width);
        string line2 = VisualizerViewport.TruncateToWidth("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║", width).PadRight(width);
        string line3 = VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width);

        // Line 4: Device | Now (combined, two labeled scroll viewports)
        const string separator = " | ";
        int leftCellWidth = (int)(width * 0.38);
        int rightCellWidth = width - leftCellWidth - separator.Length;
        if (deviceName != _deviceLastText)
        {
            _deviceScrollState.Reset();
            _deviceLastText = deviceName;
        }
        string deviceCell = ScrollingTextViewport.RenderWithLabel("Device: ", deviceName ?? "", leftCellWidth, ref _deviceScrollState, 0.25);
        string nowCell;
        if (!string.IsNullOrEmpty(nowPlayingText))
        {
            if (nowPlayingText != _nowPlayingLastText)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = nowPlayingText;
            }
            string styled = "\x1b[36m" + nowPlayingText + "\x1b[0m";
            nowCell = ScrollingTextViewport.RenderWithLabelWithAnsi("Now: ", styled, rightCellWidth, ref _nowPlayingScrollState, 0.25);
        }
        else
        {
            if (_nowPlayingLastText != null)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = null;
            }
            nowCell = ScrollingTextViewport.RenderWithLabel("Now: ", "", rightCellWidth, ref _nowPlayingScrollState, 0.25);
        }
        string line4 = deviceCell + separator + nowCell;
        int line4Visible = AnsiConsole.GetVisibleLength(line4);
        if (line4Visible < width)
        {
            line4 = AnsiConsole.PadToVisibleWidth(line4, width);
        }
        else if (line4Visible > width)
        {
            line4 = AnsiConsole.GetVisibleSubstring(line4, 0, width);
        }

        // Line 5: Mode name
        string line5 = VisualizerViewport.TruncateWithEllipsis($"Mode: {modeName ?? "Preset editor"}", width).PadRight(width);

        // Line 6: Help
        string line6 = VisualizerViewport.TruncateWithEllipsis("Press H for help, D device, F full screen, ESC quit", width).PadRight(width);

        try
        {
            WriteLineIfChanged(0, line1, ref _lastWrittenLine1);
            WriteLineIfChanged(1, line2, ref _lastWrittenLine2);
            WriteLineIfChanged(2, line3, ref _lastWrittenLine3);
            WriteLineIfChanged(3, line4, ref _lastWrittenLine4);
            WriteLineIfChanged(4, line5, ref _lastWrittenLine5);
            WriteLineIfChanged(5, line6, ref _lastWrittenLine6);
        }
        catch (Exception ex) { _ = ex; /* Console write unavailable: swallow to avoid crash */ }
    }

    private static void WriteLineIfChanged(int row, string line, ref string? lastWritten)
    {
        if (line == lastWritten)
        {
            return;
        }

        lastWritten = line;
        System.Console.SetCursorPosition(0, row);
        System.Console.Write(line);
    }
}
