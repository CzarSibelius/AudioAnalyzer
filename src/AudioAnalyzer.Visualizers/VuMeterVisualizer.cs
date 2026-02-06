using System.Text;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class VuMeterVisualizer : IVisualizer
{
    private readonly StringBuilder _lineBuffer = new(256);

    public void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow)
    {
        int termWidth = dimensions.Width;
        int termHeight = dimensions.Height;
        if (termWidth < 30 || termHeight < 15) return;

        Console.SetCursorPosition(0, displayStartRow);
        int meterWidth = Math.Min(60, termWidth - 20);
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine(BuildVuMeterChannel("  L ", snapshot.LeftChannel, snapshot.LeftPeakHold, meterWidth));
        Console.WriteLine();
        Console.WriteLine(BuildVuMeterChannel("  R ", snapshot.RightChannel, snapshot.RightPeakHold, meterWidth));
        Console.WriteLine();
        Console.WriteLine();

        _lineBuffer.Clear();
        _lineBuffer.Append("    ");
        for (int i = 0; i <= 10; i++)
            _lineBuffer.Append((i * 10).ToString().PadRight(meterWidth / 10));
        Console.WriteLine(_lineBuffer.ToString());

        _lineBuffer.Clear();
        _lineBuffer.Append("    ");
        string[] dbLabels = ["-∞", "-40", "-30", "-20", "-10", "-6", "-3", "0"];
        int labelSpacing = meterWidth / (dbLabels.Length - 1);
        for (int i = 0; i < dbLabels.Length; i++)
            _lineBuffer.Append(dbLabels[i].PadRight(labelSpacing));
        Console.WriteLine(AnsiConsole.ToAnsiString(_lineBuffer.ToString(), ConsoleColor.DarkGray));
        Console.WriteLine();

        float balance = (snapshot.RightChannel - snapshot.LeftChannel) / Math.Max(0.001f, snapshot.LeftChannel + snapshot.RightChannel);
        int balancePos = (int)((balance + 1) / 2 * meterWidth);
        _lineBuffer.Clear();
        _lineBuffer.Append("  BAL ");
        AnsiConsole.AppendColored(_lineBuffer, new string('─', balancePos), ConsoleColor.DarkGray);
        AnsiConsole.AppendColored(_lineBuffer, "●", ConsoleColor.White);
        AnsiConsole.AppendColored(_lineBuffer, new string('─', meterWidth - balancePos - 1), ConsoleColor.DarkGray);
        Console.WriteLine(_lineBuffer.ToString());

        _lineBuffer.Clear();
        _lineBuffer.Append("      L");
        _lineBuffer.Append(' ', meterWidth / 2 - 2);
        _lineBuffer.Append("C");
        _lineBuffer.Append(' ', meterWidth / 2 - 2);
        _lineBuffer.Append("R");
        Console.WriteLine(_lineBuffer.ToString());
        Console.WriteLine("\n  Classic VU Meter - Shows channel levels".PadRight(termWidth));
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
