using System.Globalization;
using System.IO;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

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

    /// <summary>Clears the console and draws the full header including device info, now-playing, BPM, Volume/db, and mode.</summary>
    /// <param name="uiSettings">Optional UI settings (title, palette, scrolling speed). When null, uses defaults.</param>
    /// <param name="currentBpm">Current detected BPM (0 when none). When >= 0, draws BPM.</param>
    /// <param name="beatSensitivity">Beat detection sensitivity.</param>
    /// <param name="beatFlashActive">Whether a beat was recently detected.</param>
    /// <param name="volume">Volume 0–1. When >= 0, draws Volume/db.</param>
    public static void DrawMain(string deviceName, string? nowPlayingText = null, string? modeName = null, UiSettings? uiSettings = null, double currentBpm = -1, double beatSensitivity = 1.3, bool beatFlashActive = false, float volume = -1)
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
        DrawHeaderOnly(deviceName, nowPlayingText, modeName, uiSettings, currentBpm, beatSensitivity, beatFlashActive, volume);
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
    /// <param name="uiSettings">Optional UI settings (title, palette, scrolling speed). When null, uses defaults.</param>
    /// <param name="currentBpm">Current detected BPM (0 when none). When >= 0, draws BPM.</param>
    /// <param name="beatSensitivity">Beat detection sensitivity.</param>
    /// <param name="beatFlashActive">Whether a beat was recently detected.</param>
    /// <param name="volume">Volume 0–1. When >= 0, draws Volume/db.</param>
    public static void DrawHeaderOnly(string deviceName, string? nowPlayingText = null, string? modeName = null, UiSettings? uiSettings = null, double currentBpm = -1, double beatSensitivity = 1.3, bool beatFlashActive = false, float volume = -1)
    {
        int width = Math.Max(10, GetConsoleWidth());
        var ui = uiSettings ?? new UiSettings();
        var palette = ui.Palette ?? new UiPalette();
        double speed = ui.DefaultScrollingSpeed;

        string titleText = " " + (string.IsNullOrWhiteSpace(ui.Title) ? "AUDIO ANALYZER - Real-time Frequency Spectrum" : ui.Title.Trim()) + " ";
        string title = StaticTextViewport.TruncateWithEllipsis(new PlainText(titleText), width - 2);
        int padding = Math.Max(0, (width - title.Length - 2) / 2);
        string line1 = StaticTextViewport.TruncateToWidth(new PlainText("╔" + new string('═', width - 2) + "╗"), width).PadRight(width);
        string line2 = StaticTextViewport.TruncateToWidth(new PlainText("║" + new string(' ', padding) + title + new string(' ', width - padding - title.Length - 2) + "║"), width).PadRight(width);
        string line3 = StaticTextViewport.TruncateToWidth(new PlainText("╚" + new string('═', width - 2) + "╝"), width).PadRight(width);

        // Line 4: Device and Now (two labeled scroll viewports; label+value colors handle separation)
        int leftCellWidth = (int)(width * 0.38);
        int rightCellWidth = width - leftCellWidth;
        if (deviceName != _deviceLastText)
        {
            _deviceScrollState.Reset();
            _deviceLastText = deviceName;
        }
        string deviceCell = ScrollingTextViewport.RenderWithLabel("Device", new PlainText(deviceName ?? ""), leftCellWidth, ref _deviceScrollState, speed, palette.Label, palette.Normal, hotkey: "D");
        string nowCell;
        if (!string.IsNullOrEmpty(nowPlayingText))
        {
            if (nowPlayingText != _nowPlayingLastText)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = nowPlayingText;
            }
            nowCell = ScrollingTextViewport.RenderWithLabel("Now: ", new PlainText(nowPlayingText), rightCellWidth, ref _nowPlayingScrollState, speed, palette.Label, palette.Highlighted);
        }
        else
        {
            if (_nowPlayingLastText != null)
            {
                _nowPlayingScrollState.Reset();
                _nowPlayingLastText = null;
            }
            nowCell = ScrollingTextViewport.RenderWithLabel("Now: ", new PlainText(""), rightCellWidth, ref _nowPlayingScrollState, speed, palette.Label, palette.Normal);
        }
        string line4 = deviceCell + nowCell;
        int line4Visible = AnsiConsole.GetVisibleLength(line4);
        if (line4Visible < width)
        {
            line4 = AnsiConsole.PadToVisibleWidth(line4, width);
        }
        else if (line4Visible > width)
        {
            line4 = AnsiConsole.GetVisibleSubstring(line4, 0, width);
        }

        // Line 5: Mode (left), BPM/Beat (middle), Volume/db (right) on same line
        int rightHalfWidth = width - leftCellWidth;
        int middleCellWidth = rightHalfWidth / 2;
        int volCellWidth = rightHalfWidth - middleCellWidth;

        string modeNameStr = modeName ?? "Preset editor";
        string modeLabel = ScrollingTextViewport.FormatLabel("Mode", "Tab");
        int modeLabelLen = new StringInfo(modeLabel).LengthInTextElements;
        string modeValueTruncated = StaticTextViewport.TruncateWithEllipsis(new PlainText(modeNameStr), leftCellWidth - modeLabelLen).TrimEnd();
        string modeCell = AnsiConsole.ColorCode(palette.Label) + modeLabel + AnsiConsole.ResetCode +
            AnsiConsole.ColorCode(palette.Normal) + modeValueTruncated + AnsiConsole.ResetCode;
        modeCell = AnsiConsole.PadToVisibleWidth(modeCell, leftCellWidth);

        string bpmCell;
        if (currentBpm >= 0)
        {
            string bpmBeatValue = currentBpm > 0
                ? $"{AnsiConsole.ColorCode(palette.Label)}BPM: {AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{currentBpm,4:F0}{AnsiConsole.ResetCode}  {AnsiConsole.ColorCode(palette.Label)}Beat: {AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{beatSensitivity,4:F1} (+/-){AnsiConsole.ResetCode}"
                : $"{AnsiConsole.ColorCode(palette.Label)}Beat: {AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{beatSensitivity,4:F1} (+/-){AnsiConsole.ResetCode}";
            if (beatFlashActive)
            {
                bpmBeatValue += " *BEAT*";
            }
            bpmCell = AnsiConsole.PadToVisibleWidth(StaticTextViewport.TruncateWithEllipsis(new AnsiText(bpmBeatValue), middleCellWidth), middleCellWidth);
        }
        else
        {
            bpmCell = new string(' ', middleCellWidth);
        }

        string volCell;
        if (volume >= 0)
        {
            double db = 20 * Math.Log10(Math.Max(volume, 0.00001));
            string volDbValue = $"{AnsiConsole.ColorCode(palette.Label)}Volume/dB: {AnsiConsole.ResetCode}{AnsiConsole.ColorCode(palette.Normal)}{volume * 100,5:F1}% {db,6:F1}dB{AnsiConsole.ResetCode}";
            volCell = AnsiConsole.PadToVisibleWidth(StaticTextViewport.TruncateWithEllipsis(new AnsiText(volDbValue), volCellWidth), volCellWidth);
        }
        else
        {
            volCell = new string(' ', volCellWidth);
        }

        string line5 = modeCell + bpmCell + volCell;
        int line5Visible = AnsiConsole.GetVisibleLength(line5);
        if (line5Visible < width)
        {
            line5 = AnsiConsole.PadToVisibleWidth(line5, width);
        }
        else if (line5Visible > width)
        {
            line5 = AnsiConsole.GetVisibleSubstring(line5, 0, width);
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
