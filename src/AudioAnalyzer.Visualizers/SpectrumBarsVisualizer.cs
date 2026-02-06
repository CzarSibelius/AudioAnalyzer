using System.Text;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class SpectrumBarsVisualizer : IVisualizer
{
    private readonly StringBuilder _lineBuffer = new(512);

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        if (viewport.Width < 30 || viewport.MaxLines < 5) return;

        int maxBarLines = Math.Max(1, viewport.MaxLines - 6);
        int barHeight = Math.Max(10, Math.Min(30, maxBarLines));
        int expectedLines = 2 + barHeight + 1 + 2; // volume(2) + bars(barHeight) + separator(1) + labels(2)

        Console.SetCursorPosition(0, viewport.StartRow);
        DisplayVolumeBar(snapshot.Volume, viewport.Width);
        DisplayFrequencyBars(snapshot, viewport);
        DisplayFrequencyLabels(snapshot, viewport);
    }

    private void DisplayVolumeBar(float volume, int termWidth)
    {
        int availableWidth = Math.Max(20, termWidth - 10);
        int volBarLength = (int)(volume * availableWidth);
        _lineBuffer.Clear();
        _lineBuffer.Append('[');
        for (int i = 0; i < availableWidth; i++)
        {
            if (i < volBarLength)
                AnsiConsole.AppendColored(_lineBuffer, "█", GetColorByPosition((double)i / availableWidth));
            else
                _lineBuffer.Append(' ');
        }
        _lineBuffer.Append(']');
        int pad = termWidth - availableWidth - 2;
        if (pad > 0) _lineBuffer.Append(' ', pad);
        Console.WriteLine(_lineBuffer.ToString());
        Console.WriteLine();
    }

    private void DisplayFrequencyBars(AnalysisSnapshot f, VisualizerViewport viewport)
    {
        int maxBarLines = Math.Max(1, viewport.MaxLines - 6);
        int barHeight = Math.Max(10, Math.Min(30, maxBarLines));
        int numBands = Math.Min(f.NumBands, Math.Max(1, (viewport.Width - 5) / 2));
        double gain = f.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / f.TargetMaxMagnitude) : 1000;
        for (int row = barHeight; row > 0; row--)
        {
            _lineBuffer.Clear();
            if (row == barHeight) _lineBuffer.Append("100%");
            else if (row == (int)(barHeight * 0.75) && barHeight >= 16) _lineBuffer.Append(" 75%");
            else if (row == (int)(barHeight * 0.5) && barHeight >= 12) _lineBuffer.Append(" 50%");
            else if (row == (int)(barHeight * 0.25) && barHeight >= 16) _lineBuffer.Append(" 25%");
            else if (row == 1) _lineBuffer.Append("  0%");
            else _lineBuffer.Append("    ");
            _lineBuffer.Append(' ');
            for (int band = 0; band < numBands; band++)
            {
                double normalizedMag = Math.Min(f.SmoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                double normalizedPeak = Math.Min(f.PeakHold[band] * gain * 0.8, 1.0);
                int peakHeight = (int)(normalizedPeak * barHeight);
                if (row == peakHeight && peakHeight > 0)
                    AnsiConsole.AppendColored(_lineBuffer, "══", ConsoleColor.White);
                else if (height >= row)
                    AnsiConsole.AppendColored(_lineBuffer, "██", GetColorByRow(row, barHeight));
                else
                    _lineBuffer.Append("  ");
            }
            Console.WriteLine(_lineBuffer.ToString());
        }
        _lineBuffer.Clear();
        _lineBuffer.Append("     ");
        _lineBuffer.Append('─', Math.Min(numBands * 2, viewport.Width - 6));
        Console.WriteLine(_lineBuffer.ToString());
    }

    private static ConsoleColor GetColorByPosition(double position)
    {
        return position switch
        {
            >= 0.85 => ConsoleColor.Red, >= 0.7 => ConsoleColor.Magenta, >= 0.55 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green, >= 0.25 => ConsoleColor.Cyan, _ => ConsoleColor.Blue
        };
    }

    private static ConsoleColor GetColorByRow(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        return position switch
        {
            <= 0.25 => ConsoleColor.Red, <= 0.4 => ConsoleColor.Magenta, <= 0.55 => ConsoleColor.Yellow,
            <= 0.7 => ConsoleColor.Green, <= 0.85 => ConsoleColor.Cyan, _ => ConsoleColor.Blue
        };
    }

    private void DisplayFrequencyLabels(AnalysisSnapshot f, VisualizerViewport viewport)
    {
        int numBands = Math.Min(f.NumBands, Math.Max(1, (viewport.Width - 5) / 2));
        _lineBuffer.Clear();
        _lineBuffer.Append("     ");
        var allLabels = new[] { "20", "30", "50", "80", "100", "150", "200", "300", "500", "800",
            "1k", "1.5k", "2k", "3k", "5k", "8k", "10k", "15k", "20k" };
        int maxLabels = Math.Max(4, Math.Min(allLabels.Length, numBands / 3));
        int labelInterval = Math.Max(1, numBands / maxLabels);
        for (int band = 0; band < numBands && band * 2 < viewport.Width - 6; band++)
        {
            if (band % labelInterval == 0 && band / labelInterval < allLabels.Length)
            {
                string label = allLabels[Math.Min(band / labelInterval, allLabels.Length - 1)];
                _lineBuffer.Append(label.PadRight(Math.Min(4, labelInterval * 2))[..Math.Min(label.Length + 1, 2)]);
            }
            else
                _lineBuffer.Append("  ");
        }
        Console.WriteLine(_lineBuffer.ToString());
        Console.WriteLine(VisualizerViewport.TruncateToWidth("\n Frequency (Hz)".PadRight(viewport.Width), viewport.Width));
    }
}
