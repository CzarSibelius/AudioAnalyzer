using System.Text;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class WinampBarsVisualizer : IVisualizer
{
    private readonly StringBuilder _lineBuffer = new(512);

    public void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow)
    {
        int termWidth = dimensions.Width;
        int termHeight = dimensions.Height;
        if (termWidth < 30 || termHeight < 15) return;

        Console.SetCursorPosition(0, displayStartRow);
        int barHeight = Math.Max(10, Math.Min(20, termHeight - 14));
        int numBars = Math.Min(snapshot.NumBands, (termWidth - 4) / 3);
        Console.WriteLine();
        double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
        for (int row = barHeight; row >= 1; row--)
        {
            _lineBuffer.Clear();
            _lineBuffer.Append("  ");
            for (int band = 0; band < numBars; band++)
            {
                double normalizedMag = Math.Min(snapshot.SmoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                double normalizedPeak = Math.Min(snapshot.PeakHold[band] * gain * 0.8, 1.0);
                int peakH = (int)(normalizedPeak * barHeight);
                if (row == peakH && peakH > 0)
                    AnsiConsole.AppendColored(_lineBuffer, "▀▀", ConsoleColor.White);
                else if (height >= row)
                    AnsiConsole.AppendColored(_lineBuffer, "██", GetWinampColor(row, barHeight));
                else
                    _lineBuffer.Append("  ");
                _lineBuffer.Append(' ');
            }
            Console.WriteLine(_lineBuffer.ToString());
        }
        _lineBuffer.Clear();
        _lineBuffer.Append("  ");
        for (int band = 0; band < numBars; band++)
            AnsiConsole.AppendColored(_lineBuffer, "══ ", ConsoleColor.DarkGray);
        Console.WriteLine(_lineBuffer.ToString());
        Console.WriteLine("\n  Winamp Style - Classic music player visualization".PadRight(termWidth));
    }

    private static ConsoleColor GetWinampColor(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        return position switch
        {
            >= 0.85 => ConsoleColor.Red, >= 0.7 => ConsoleColor.DarkYellow, >= 0.5 => ConsoleColor.Yellow,
            >= 0.3 => ConsoleColor.Green, _ => ConsoleColor.DarkGreen
        };
    }
}
