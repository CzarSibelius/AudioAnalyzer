using System.IO;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Renders the application header (title, device name, shortcuts) to the console.</summary>
internal static class ConsoleHeader
{
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

    /// <summary>Clears the console and draws the full header including device info, now-playing, BPM, and Volume/db.</summary>
    /// <param name="titleBar">Injectable title bar renderer for the app/mode/preset/layer breadcrumb.</param>
    /// <param name="deviceViewport">Viewport for device name scrolling.</param>
    /// <param name="nowPlayingViewport">Viewport for now-playing text scrolling.</param>
    /// <param name="uiSettings">Optional UI settings (title, palette, scrolling speed). When null, uses defaults.</param>
    /// <param name="currentBpm">Current detected BPM (0 when none). When >= 0, draws BPM.</param>
    /// <param name="beatSensitivity">Beat detection sensitivity.</param>
    /// <param name="beatFlashActive">Whether a beat was recently detected.</param>
    /// <param name="volume">Volume 0–1. When >= 0, draws Volume/db.</param>
    public static void DrawMain(string deviceName, ITitleBarRenderer titleBar, IScrollingTextViewport deviceViewport, IScrollingTextViewport nowPlayingViewport, string? nowPlayingText = null, UiSettings? uiSettings = null, double currentBpm = -1, double beatSensitivity = 1.3, bool beatFlashActive = false, float volume = -1)
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
        DrawHeaderOnly(deviceName, titleBar, deviceViewport, nowPlayingViewport, nowPlayingText, uiSettings, currentBpm, beatSensitivity, beatFlashActive, volume);
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
    /// <param name="titleBar">Injectable title bar renderer for the app/mode/preset/layer breadcrumb.</param>
    /// <param name="deviceViewport">Viewport for device name scrolling.</param>
    /// <param name="nowPlayingViewport">Viewport for now-playing text scrolling.</param>
    /// <param name="nowPlayingText">Optional now-playing text from system media session (e.g. "Artist - Title").</param>
    /// <param name="uiSettings">Optional UI settings (title, palette, scrolling speed). When null, uses defaults.</param>
    /// <param name="currentBpm">Current detected BPM (0 when none). When >= 0, draws BPM.</param>
    /// <param name="beatSensitivity">Beat detection sensitivity.</param>
    /// <param name="beatFlashActive">Whether a beat was recently detected.</param>
    /// <param name="volume">Volume 0–1. When >= 0, draws Volume/db.</param>
    public static void DrawHeaderOnly(string deviceName, ITitleBarRenderer titleBar, IScrollingTextViewport deviceViewport, IScrollingTextViewport nowPlayingViewport, string? nowPlayingText = null, UiSettings? uiSettings = null, double currentBpm = -1, double beatSensitivity = 1.3, bool beatFlashActive = false, float volume = -1)
    {
        int width = Math.Max(10, GetConsoleWidth());
        var ui = uiSettings ?? new UiSettings();
        var palette = ui.Palette ?? new UiPalette();
        double speed = ui.DefaultScrollingSpeed;

        var (line1, line2, line3) = titleBar.Render(width);
        line1 = AnsiConsole.PadToDisplayWidth(StaticTextViewport.TruncateToWidth(new AnsiText(line1), width), width);
        line2 = AnsiConsole.PadToDisplayWidth(StaticTextViewport.TruncateToWidth(new AnsiText(line2), width), width);
        line3 = AnsiConsole.PadToDisplayWidth(StaticTextViewport.TruncateToWidth(new AnsiText(line3), width), width);

        // Line 4: Device and Now (two labeled scroll viewports; label+value colors handle separation)
        int leftCellWidth = (int)(width * 0.38);
        int rightCellWidth = width - leftCellWidth;
        string deviceCell = deviceViewport.RenderWithLabel("Device", new PlainText(deviceName ?? ""), leftCellWidth, speed, palette.Label, palette.Normal, hotkey: "D");
        string nowCell = !string.IsNullOrEmpty(nowPlayingText)
            ? nowPlayingViewport.RenderWithLabel("Now:", new PlainText(nowPlayingText), rightCellWidth, speed, palette.Label, palette.Highlighted)
            : nowPlayingViewport.RenderWithLabel("Now:", new PlainText(""), rightCellWidth, speed, palette.Label, palette.Normal);
        string line4 = deviceCell + nowCell;
        int line4DisplayWidth = AnsiConsole.GetDisplayWidth(line4);
        if (line4DisplayWidth < width)
        {
            line4 = AnsiConsole.PadToDisplayWidth(line4, width);
        }
        else if (line4DisplayWidth > width)
        {
            line4 = AnsiConsole.GetDisplaySubstring(line4, 0, width);
        }

        // Line 5: BPM/Beat (left), Volume/db (right) on same line
        int bpmCellWidth = width / 2;
        int volCellWidth = width - bpmCellWidth;

        string bpmCell;
        if (currentBpm >= 0)
        {
            string bpmBeatValue = currentBpm > 0
                ? $"{AnsiConsole.ColorCode(palette.Label)}BPM:{AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{currentBpm,4:F0}{AnsiConsole.ResetCode}  {AnsiConsole.ColorCode(palette.Label)}Beat:{AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{beatSensitivity,4:F1} (+/-){AnsiConsole.ResetCode}"
                : $"{AnsiConsole.ColorCode(palette.Label)}Beat:{AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{beatSensitivity,4:F1} (+/-){AnsiConsole.ResetCode}";
            if (beatFlashActive)
            {
                bpmBeatValue += " *BEAT*";
            }
            bpmCell = AnsiConsole.PadToDisplayWidth(StaticTextViewport.TruncateWithEllipsis(new AnsiText(bpmBeatValue), bpmCellWidth), bpmCellWidth);
        }
        else
        {
            bpmCell = new string(' ', bpmCellWidth);
        }

        string volCell;
        if (volume >= 0)
        {
            double db = 20 * Math.Log10(Math.Max(volume, 0.00001));
            string volDbValue = $"{AnsiConsole.ColorCode(palette.Label)}Volume/dB:{AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{volume * 100,5:F1}% {db,6:F1}dB{AnsiConsole.ResetCode}";
            volCell = AnsiConsole.PadToDisplayWidth(StaticTextViewport.TruncateWithEllipsis(new AnsiText(volDbValue), volCellWidth), volCellWidth);
        }
        else
        {
            volCell = new string(' ', volCellWidth);
        }

        string line5 = bpmCell + volCell;
        int line5DisplayWidth = AnsiConsole.GetDisplayWidth(line5);
        if (line5DisplayWidth < width)
        {
            line5 = AnsiConsole.PadToDisplayWidth(line5, width);
        }
        else if (line5DisplayWidth > width)
        {
            line5 = AnsiConsole.GetDisplaySubstring(line5, 0, width);
        }

        // Line 6: Help (Dimmed per ADR-0033)
        string line6Raw = StaticTextViewport.TruncateWithEllipsis(new PlainText("Press H for help, D device, F full screen, ESC quit"), width).PadRight(width);
        string line6 = AnsiConsole.ColorCode(palette.Dimmed) + line6Raw + AnsiConsole.ResetCode;

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
