using System.Text;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class VuMeterVisualizer : IVisualizer
{
    public string TechnicalName => "vumeter";
    public string DisplayName => "VU Meter";
    public bool SupportsPaletteCycling => false;

    private readonly StringBuilder _lineBuffer = new(256);

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        if (viewport.Width < 30 || viewport.MaxLines < 7) return;

        int meterWidth = Math.Min(60, viewport.Width - 20);
        int lineCount = 0;
        void WriteLineSafe(string s)
        {
            if (lineCount < viewport.MaxLines)
            {
                string line = s.Contains("\x1b") ? s : VisualizerViewport.TruncateToWidth(s, viewport.Width);
                Console.WriteLine(line);
                lineCount++;
            }
        }

        Console.SetCursorPosition(0, viewport.StartRow);
        WriteLineSafe("");
        WriteLineSafe("");
        WriteLineSafe(BuildVuMeterChannel("  L ", snapshot.LeftChannel, snapshot.LeftPeakHold, meterWidth));
        WriteLineSafe("");
        WriteLineSafe(BuildVuMeterChannel("  R ", snapshot.RightChannel, snapshot.RightPeakHold, meterWidth));
        WriteLineSafe("");
        WriteLineSafe("");

        _lineBuffer.Clear();
        _lineBuffer.Append("    ");
        for (int i = 0; i <= 10; i++)
            _lineBuffer.Append((i * 10).ToString().PadRight(Math.Max(1, meterWidth / 10)));
        WriteLineSafe(_lineBuffer.ToString());

        _lineBuffer.Clear();
        _lineBuffer.Append("    ");
        string[] dbLabels = ["-∞", "-40", "-30", "-20", "-10", "-6", "-3", "0"];
        int labelSpacing = Math.Max(1, meterWidth / (dbLabels.Length - 1));
        for (int i = 0; i < dbLabels.Length; i++)
            _lineBuffer.Append(dbLabels[i].PadRight(labelSpacing));
        WriteLineSafe(AnsiConsole.ToAnsiString(_lineBuffer.ToString(), ConsoleColor.DarkGray));
        WriteLineSafe("");

        float balance = (snapshot.RightChannel - snapshot.LeftChannel) / Math.Max(0.001f, snapshot.LeftChannel + snapshot.RightChannel);
        int balancePos = (int)((balance + 1) / 2 * meterWidth);
        balancePos = Math.Clamp(balancePos, 0, meterWidth - 1);
        _lineBuffer.Clear();
        _lineBuffer.Append("  BAL ");
        AnsiConsole.AppendColored(_lineBuffer, new string('─', balancePos), ConsoleColor.DarkGray);
        AnsiConsole.AppendColored(_lineBuffer, "●", ConsoleColor.White);
        AnsiConsole.AppendColored(_lineBuffer, new string('─', meterWidth - balancePos - 1), ConsoleColor.DarkGray);
        WriteLineSafe(_lineBuffer.ToString());

        _lineBuffer.Clear();
        _lineBuffer.Append("      L");
        _lineBuffer.Append(' ', Math.Max(0, meterWidth / 2 - 2));
        _lineBuffer.Append("C");
        _lineBuffer.Append(' ', Math.Max(0, meterWidth / 2 - 2));
        _lineBuffer.Append("R");
        WriteLineSafe(_lineBuffer.ToString());
        WriteLineSafe("\n  Classic VU Meter - Shows channel levels".PadRight(viewport.Width));
    }

    private string BuildVuMeterChannel(string label, float level, float peakHold, int width)
    {
        _lineBuffer.Clear();
        _lineBuffer.Append(label);
        _lineBuffer.Append('[');
        int barLength = (int)(level * width);
        int peakPos = (int)(peakHold * width);
        for (int i = 0; i < width; i++)
        {
            if (i == peakPos && peakPos > 0)
                AnsiConsole.AppendColored(_lineBuffer, "│", ConsoleColor.White);
            else if (i < barLength)
                AnsiConsole.AppendColored(_lineBuffer, "█", GetVuColor((double)i / width));
            else
                AnsiConsole.AppendColored(_lineBuffer, "░", ConsoleColor.DarkGray);
        }
        _lineBuffer.Append(']');
        double db = 20 * Math.Log10(Math.Max(level, 0.00001));
        _lineBuffer.Append($" {db:F1} dB");
        return _lineBuffer.ToString();
    }

    private static ConsoleColor GetVuColor(double position) =>
        position switch { >= 0.9 => ConsoleColor.Red, >= 0.75 => ConsoleColor.Yellow, _ => ConsoleColor.Green };
}
