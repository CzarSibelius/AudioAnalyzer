using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class VuMeterVisualizer : IVisualizer
{
    public void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow)
    {
        int termWidth = dimensions.Width;
        int termHeight = dimensions.Height;
        if (termWidth < 30 || termHeight < 15) return;

        Console.SetCursorPosition(0, displayStartRow);
        int meterWidth = Math.Min(60, termWidth - 20);
        Console.WriteLine();
        Console.WriteLine();
        DrawVuMeterChannel("  L ", snapshot.LeftChannel, snapshot.LeftPeakHold, meterWidth);
        Console.WriteLine();
        DrawVuMeterChannel("  R ", snapshot.RightChannel, snapshot.RightPeakHold, meterWidth);
        Console.WriteLine();
        Console.WriteLine();
        Console.Write("    ");
        for (int i = 0; i <= 10; i++)
            Console.Write((i * 10).ToString().PadRight(meterWidth / 10));
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("    ");
        string[] dbLabels = { "-∞", "-40", "-30", "-20", "-10", "-6", "-3", "0" };
        int labelSpacing = meterWidth / (dbLabels.Length - 1);
        for (int i = 0; i < dbLabels.Length; i++)
            Console.Write(dbLabels[i].PadRight(labelSpacing));
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine();
        float balance = (snapshot.RightChannel - snapshot.LeftChannel) / Math.Max(0.001f, snapshot.LeftChannel + snapshot.RightChannel);
        int balancePos = (int)((balance + 1) / 2 * meterWidth);
        Console.Write("  BAL ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('─', balancePos));
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("●");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', meterWidth - balancePos - 1));
        Console.Write("      L");
        Console.Write(new string(' ', meterWidth / 2 - 2));
        Console.Write("C");
        Console.Write(new string(' ', meterWidth / 2 - 2));
        Console.WriteLine("R");
        Console.ResetColor();
        Console.WriteLine("\n  Classic VU Meter - Shows channel levels".PadRight(termWidth));
    }

    private static void DrawVuMeterChannel(string label, float level, float peakHold, int width)
    {
        Console.Write(label);
        Console.Write("[");
        int barLength = (int)(level * width);
        int peakPos = (int)(peakHold * width);
        for (int i = 0; i < width; i++)
        {
            if (i == peakPos && peakPos > 0) { Console.ForegroundColor = ConsoleColor.White; Console.Write("│"); }
            else if (i < barLength) { SetVuColor((double)i / width); Console.Write("█"); }
            else { Console.ForegroundColor = ConsoleColor.DarkGray; Console.Write("░"); }
            Console.ResetColor();
        }
        Console.Write("]");
        double db = 20 * Math.Log10(Math.Max(level, 0.00001));
        Console.Write($" {db:F1} dB");
    }

    private static void SetVuColor(double position)
    {
        Console.ForegroundColor = position switch { >= 0.9 => ConsoleColor.Red, >= 0.75 => ConsoleColor.Yellow, _ => ConsoleColor.Green };
    }
}
