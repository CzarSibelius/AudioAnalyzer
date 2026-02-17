using System.IO;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>Renders the application header (title, device name, shortcuts) to the console.</summary>
internal static class ConsoleHeader
{
    private static ScrollingTextViewportState _nowPlayingScrollState;
    private static string? _nowPlayingLastText;

    /// <summary>Gets the current console width, or 80 if unavailable.</summary>
    public static int GetConsoleWidth()
    {
        try { return System.Console.WindowWidth; }
        catch (IOException) { return 80; }
    }

    /// <summary>Clears the console and draws the full header including device info and optional now-playing text.</summary>
    public static void DrawMain(string deviceName, string? nowPlayingText = null)
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
        DrawHeaderOnly(deviceName, nowPlayingText);
    }

    /// <summary>Draws only the header lines (no clear). Used for refresh before each render.</summary>
    /// <param name="deviceName">Display name of the current audio input device.</param>
    /// <param name="nowPlayingText">Optional now-playing text from system media session (e.g. "Artist - Title").</param>
    public static void DrawHeaderOnly(string deviceName, string? nowPlayingText = null)
    {
        int width = Math.Max(10, GetConsoleWidth());
        string title = " AUDIO ANALYZER - Real-time Frequency Spectrum ";
        title = VisualizerViewport.TruncateWithEllipsis(title, width - 2);
        int padding = Math.Max(0, (width - title.Length - 2) / 2);
        string line1 = VisualizerViewport.TruncateToWidth("╔" + new string('═', width - 2) + "╗", width).PadRight(width);
        string line2 = VisualizerViewport.TruncateToWidth("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║", width).PadRight(width);
        string line3 = VisualizerViewport.TruncateToWidth("╚" + new string('═', width - 2) + "╝", width).PadRight(width);
        string line4 = VisualizerViewport.TruncateWithEllipsis($"Input: {deviceName}", width).PadRight(width);
        string line5 = VisualizerViewport.TruncateWithEllipsis("Press H for help, D device, F full screen, ESC quit", width).PadRight(width);
        string line6;
        if (!string.IsNullOrEmpty(nowPlayingText))
        {
            if (nowPlayingText != _nowPlayingLastText)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = nowPlayingText;
            }
            string prefix = "\x1b[36m"; // DarkCyan
            string suffix = "\x1b[0m";
            string styled = prefix + nowPlayingText + suffix;
            int visibleLen = AnsiConsole.GetVisibleLength(styled);
            line6 = visibleLen > width
                ? ScrollingTextViewport.RenderWithAnsi(styled, width, ref _nowPlayingScrollState, 0.25)
                : AnsiConsole.PadToVisibleWidth(styled, width);
        }
        else
        {
            if (_nowPlayingLastText != null)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = null;
            }
            line6 = new string(' ', width);
        }

        try
        {
            System.Console.SetCursorPosition(0, 0);
            System.Console.Write(line1);
            System.Console.SetCursorPosition(0, 1);
            System.Console.Write(line2);
            System.Console.SetCursorPosition(0, 2);
            System.Console.Write(line3);
            System.Console.SetCursorPosition(0, 3);
            System.Console.Write(line4);
            System.Console.SetCursorPosition(0, 4);
            System.Console.Write(line5);
            System.Console.SetCursorPosition(0, 5);
            System.Console.Write(line6);
        }
        catch (Exception ex) { _ = ex; /* Console write unavailable: swallow to avoid crash */ }
    }
}
