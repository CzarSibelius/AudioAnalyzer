using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class SpectrumBarsVisualizer : IVisualizer
{
    public void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow)
    {
        int termWidth = dimensions.Width;
        int termHeight = dimensions.Height;
        if (termWidth < 30 || termHeight < 15) return;

        Console.SetCursorPosition(0, displayStartRow);
        DisplayVolumeBar(snapshot.Volume, termWidth);
        DisplayFrequencyBars(snapshot, termWidth, termHeight);
        DisplayFrequencyLabels(snapshot, termWidth);
    }

    private static void DisplayVolumeBar(float volume, int termWidth)
    {
        int availableWidth = Math.Max(20, termWidth - 10);
        int volBarLength = (int)(volume * availableWidth);
        Console.Write("[");
        for (int i = 0; i < availableWidth; i++)
        {
            if (i < volBarLength)
            {
                SetColorByPosition((double)i / availableWidth);
                Console.Write("█");
                Console.ResetColor();
            }
            else Console.Write(" ");
        }
        Console.WriteLine("]".PadRight(termWidth - availableWidth - 1));
        Console.WriteLine();
    }

    private static void DisplayFrequencyBars(AnalysisSnapshot f, int termWidth, int termHeight)
    {
        int barHeight = Math.Max(10, Math.Min(30, termHeight - 15));
        double gain = f.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / f.TargetMaxMagnitude) : 1000;
        for (int row = barHeight; row > 0; row--)
        {
            if (row == barHeight) Console.Write("100%");
            else if (row == (int)(barHeight * 0.75) && barHeight >= 16) Console.Write(" 75%");
            else if (row == (int)(barHeight * 0.5) && barHeight >= 12) Console.Write(" 50%");
            else if (row == (int)(barHeight * 0.25) && barHeight >= 16) Console.Write(" 25%");
            else if (row == 1) Console.Write("  0%");
            else Console.Write("    ");
            Console.Write(" ");
            for (int band = 0; band < f.NumBands; band++)
            {
                double normalizedMag = Math.Min(f.SmoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                double normalizedPeak = Math.Min(f.PeakHold[band] * gain * 0.8, 1.0);
                int peakHeight = (int)(normalizedPeak * barHeight);
                if (row == peakHeight && peakHeight > 0) { Console.ForegroundColor = ConsoleColor.White; Console.Write("══"); Console.ResetColor(); }
                else if (height >= row) { SetColorByRow(row, barHeight); Console.Write("██"); Console.ResetColor(); }
                else Console.Write("  ");
            }
            Console.WriteLine();
        }
        Console.Write("     ");
        Console.WriteLine(new string('─', Math.Min(f.NumBands * 2, termWidth - 6)));
    }

    private static void SetColorByPosition(double position)
    {
        Console.ForegroundColor = position switch
        {
            >= 0.85 => ConsoleColor.Red, >= 0.7 => ConsoleColor.Magenta, >= 0.55 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green, >= 0.25 => ConsoleColor.Cyan, _ => ConsoleColor.Blue
        };
    }

    private static void SetColorByRow(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        Console.ForegroundColor = position switch
        {
            <= 0.25 => ConsoleColor.Red, <= 0.4 => ConsoleColor.Magenta, <= 0.55 => ConsoleColor.Yellow,
            <= 0.7 => ConsoleColor.Green, <= 0.85 => ConsoleColor.Cyan, _ => ConsoleColor.Blue
        };
    }

    private static void DisplayFrequencyLabels(AnalysisSnapshot f, int termWidth)
    {
        Console.Write("     ");
        var allLabels = new[] { "20", "30", "50", "80", "100", "150", "200", "300", "500", "800",
            "1k", "1.5k", "2k", "3k", "5k", "8k", "10k", "15k", "20k" };
        int maxLabels = Math.Max(4, Math.Min(allLabels.Length, f.NumBands / 3));
        int labelInterval = Math.Max(1, f.NumBands / maxLabels);
        for (int band = 0; band < f.NumBands && band * 2 < termWidth - 6; band++)
        {
            if (band % labelInterval == 0 && band / labelInterval < allLabels.Length)
            {
                string label = allLabels[Math.Min(band / labelInterval, allLabels.Length - 1)];
                Console.Write(label.PadRight(Math.Min(4, labelInterval * 2))[..Math.Min(label.Length + 1, 2)]);
            }
            else Console.Write("  ");
        }
        Console.WriteLine();
        Console.WriteLine("\n Frequency (Hz)".PadRight(termWidth));
    }
}
