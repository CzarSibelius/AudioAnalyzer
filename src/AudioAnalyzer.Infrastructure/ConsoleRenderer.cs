using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class ConsoleRenderer : IVisualizationRenderer
{
    private static readonly ConsoleColor[] BeatCircleColors = [
        ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.Yellow,
        ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Blue
    ];

    public void Render(VisualizationFrame f)
    {
        try
        {
            if (f.TerminalWidth < 30 || f.TerminalHeight < 15) return;
            Console.SetCursorPosition(0, f.DisplayStartRow);
            int termWidth = f.TerminalWidth;
            DisplayVolumeInfo(f, termWidth);
            switch (f.Mode)
            {
                case VisualizationMode.SpectrumBars:
                    DisplayVolumeBar(f.Volume, termWidth);
                    DisplayFrequencyBars(f, termWidth);
                    DisplayFrequencyLabels(f, termWidth);
                    break;
                case VisualizationMode.Oscilloscope:
                    DisplayOscilloscope(f, termWidth);
                    break;
                case VisualizationMode.VuMeter:
                    DisplayVuMeter(f, termWidth);
                    break;
                case VisualizationMode.WinampBars:
                    DisplayWinampBars(f, termWidth);
                    break;
                case VisualizationMode.Geiss:
                    DisplayGeiss(f, termWidth);
                    break;
            }
        }
        catch { }
    }

    private static void DisplayVolumeInfo(VisualizationFrame f, int termWidth)
    {
        double db = 20 * Math.Log10(Math.Max(f.Volume, 0.00001));
        string bpmDisplay = f.CurrentBpm > 0 ? $" | BPM: {f.CurrentBpm:F0}" : "";
        string sensitivityDisplay = $" | Beat: {f.BeatSensitivity:F1} (+/-)";
        string beatIndicator = f.BeatFlashActive ? " *BEAT*" : "";
        if (f.BeatFlashActive) Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Volume: {f.Volume * 100:F1}% ({db:F1} dB){bpmDisplay}{sensitivityDisplay}{beatIndicator}".PadRight(termWidth));
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"Mode: {f.ModeName} (V) | S=Save | H=Help".PadRight(termWidth));
        Console.ResetColor();
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

    private static void DisplayOscilloscope(VisualizationFrame f, int termWidth)
    {
        int height = Math.Max(10, Math.Min(25, f.TerminalHeight - 12));
        int width = Math.Min(termWidth - 4, f.WaveformSize);
        int centerY = height / 2;
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ┌" + new string('─', width) + "┐");
        Console.ResetColor();
        var screen = new char[height, width];
        var colors = new ConsoleColor[height, width];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++) { screen[y, x] = ' '; colors[y, x] = ConsoleColor.Gray; }
        for (int x = 0; x < width; x++) { screen[centerY, x] = '·'; colors[centerY, x] = ConsoleColor.DarkGray; }
        int step = Math.Max(1, f.WaveformSize / width);
        int prevY = centerY;
        for (int x = 0; x < width; x++)
        {
            int sampleIndex = (f.WaveformPosition + x * step) % f.WaveformSize;
            float sample = f.Waveform[sampleIndex];
            int y = centerY - (int)(sample * (height / 2 - 1));
            y = Math.Clamp(y, 0, height - 1);
            int minY = Math.Min(prevY, y), maxY = Math.Max(prevY, y);
            for (int lineY = minY; lineY <= maxY; lineY++)
            {
                screen[lineY, x] = '█';
                colors[lineY, x] = GetOscilloscopeColor(lineY, centerY, height);
            }
            prevY = y;
        }
        for (int y = 0; y < height; y++)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  │");
            for (int x = 0; x < width; x++)
            {
                Console.ForegroundColor = colors[y, x];
                Console.Write(screen[y, x]);
            }
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("│");
        }
        Console.WriteLine("  └" + new string('─', width) + "┘");
        Console.ResetColor();
        Console.WriteLine("\n  Waveform Display - Shows audio amplitude over time".PadRight(termWidth));
    }

    private static ConsoleColor GetOscilloscopeColor(int y, int centerY, int height)
    {
        double distance = Math.Abs(y - centerY) / (double)(height / 2);
        return distance switch { >= 0.8 => ConsoleColor.Red, >= 0.6 => ConsoleColor.Yellow, >= 0.4 => ConsoleColor.Green, _ => ConsoleColor.Cyan };
    }

    private static void DisplayVuMeter(VisualizationFrame f, int termWidth)
    {
        int meterWidth = Math.Min(60, termWidth - 20);
        Console.WriteLine();
        Console.WriteLine();
        DrawVuMeterChannel("  L ", f.LeftChannel, f.LeftPeakHold, meterWidth);
        Console.WriteLine();
        DrawVuMeterChannel("  R ", f.RightChannel, f.RightPeakHold, meterWidth);
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
        float balance = (f.RightChannel - f.LeftChannel) / Math.Max(0.001f, f.LeftChannel + f.RightChannel);
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

    private static void DisplayWinampBars(VisualizationFrame f, int termWidth)
    {
        int barHeight = Math.Max(10, Math.Min(20, f.TerminalHeight - 14));
        int numBars = Math.Min(f.NumBands, (termWidth - 4) / 3);
        Console.WriteLine();
        double gain = f.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / f.TargetMaxMagnitude) : 1000;
        for (int row = barHeight; row >= 1; row--)
        {
            Console.Write("  ");
            for (int band = 0; band < numBars; band++)
            {
                double normalizedMag = Math.Min(f.SmoothedMagnitudes[band] * gain * 0.8, 1.0);
                int height = (int)(normalizedMag * barHeight);
                double normalizedPeak = Math.Min(f.PeakHold[band] * gain * 0.8, 1.0);
                int peakH = (int)(normalizedPeak * barHeight);
                if (row == peakH && peakH > 0) { Console.ForegroundColor = ConsoleColor.White; Console.Write("▀▀"); }
                else if (height >= row) { SetWinampColor(row, barHeight); Console.Write("██"); }
                else Console.Write("  ");
                Console.ResetColor();
                Console.Write(" ");
            }
            Console.WriteLine();
        }
        Console.Write("  ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        for (int band = 0; band < numBars; band++) Console.Write("══ ");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("\n  Winamp Style - Classic music player visualization".PadRight(termWidth));
    }

    private static void SetWinampColor(int row, int barHeight)
    {
        double position = (double)row / barHeight;
        Console.ForegroundColor = position switch
        {
            >= 0.85 => ConsoleColor.Red, >= 0.7 => ConsoleColor.DarkYellow, >= 0.5 => ConsoleColor.Yellow,
            >= 0.3 => ConsoleColor.Green, _ => ConsoleColor.DarkGreen
        };
    }

    private static void DisplayGeiss(VisualizationFrame f, int termWidth)
    {
        int height = Math.Max(12, Math.Min(25, f.TerminalHeight - 12));
        int width = Math.Min(termWidth - 4, 100);
        char[] plasmaChars = [' ', '·', ':', ';', '+', '*', '#', '@', '█'];
        Console.Write(new string(' ', termWidth - 1));
        Console.WriteLine();
        for (int y = 0; y < height; y++)
        {
            Console.Write("  ");
            for (int x = 0; x < width; x++)
            {
                double nx = (double)x / width, ny = (double)y / height;
                bool onCircle = false;
                ConsoleColor circleColor = ConsoleColor.White;
                if (f.ShowBeatCircles)
                {
                    double aspectRatio = 2.0;
                    double distFromCenter = Math.Sqrt((nx - 0.5) * (nx - 0.5) + ((ny - 0.5) / aspectRatio) * ((ny - 0.5) / aspectRatio));
                    foreach (var circle in f.BeatCircles)
                    {
                        double thickness = 0.02 + (1.0 - (double)circle.Age / 30) * 0.01;
                        if (Math.Abs(distFromCenter - circle.Radius) < thickness)
                        {
                            onCircle = true;
                            circleColor = BeatCircleColors[circle.ColorIndex % BeatCircleColors.Length];
                            break;
                        }
                    }
                }
                if (onCircle)
                {
                    Console.ForegroundColor = circleColor;
                    Console.Write("○");
                }
                else
                {
                    double v1 = Math.Sin(nx * 10 + f.GeissPhase);
                    double v2 = Math.Sin(ny * 8 + f.GeissPhase * 0.7);
                    double v3 = Math.Sin((nx + ny) * 6 + f.GeissPhase * 1.3);
                    double v4 = Math.Sin(Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5)) * 12 + f.GeissPhase);
                    double plasma = (v1 + v2 + v3 + v4) / 4.0;
                    double distFromCenterPlasma = Math.Sqrt((nx - 0.5) * (nx - 0.5) + (ny - 0.5) * (ny - 0.5));
                    plasma += Math.Sin(distFromCenterPlasma * 20 - f.GeissPhase * 2) * f.GeissBassIntensity * 0.5;
                    plasma += Math.Sin(nx * 30 + ny * 30 + f.GeissPhase * 3) * f.GeissTrebleIntensity * 0.3;
                    if (f.BeatFlashActive) plasma += 0.3;
                    plasma = Math.Clamp((plasma + 1.5) / 3.0, 0, 1);
                    double hue = ((nx + ny + f.GeissColorPhase) + plasma * 0.3) % 1.0;
                    Console.ForegroundColor = GetGeissColor(hue, plasma);
                    Console.Write(plasmaChars[(int)(plasma * (plasmaChars.Length - 1))]);
                }
            }
            Console.ResetColor();
            int remaining = termWidth - width - 3;
            if (remaining > 0) Console.Write(new string(' ', remaining));
            Console.WriteLine();
        }
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  Geiss - Psychedelic | Bass: {f.GeissBassIntensity:F2} | Treble: {f.GeissTrebleIntensity:F2}".PadRight(termWidth - 1));
        Console.ResetColor();
        Console.WriteLine();
    }

    private static ConsoleColor GetGeissColor(double hue, double intensity)
    {
        if (intensity < 0.2) return ConsoleColor.DarkBlue;
        int colorIndex = (int)(hue * 12) % 12;
        return colorIndex switch
        {
            0 => ConsoleColor.Red, 1 => ConsoleColor.DarkYellow, 2 => ConsoleColor.Yellow, 3 => ConsoleColor.Green,
            4 => ConsoleColor.Cyan, 5 => ConsoleColor.Blue, 6 => ConsoleColor.DarkBlue, 7 => ConsoleColor.Magenta,
            8 => ConsoleColor.DarkMagenta, 9 => ConsoleColor.Red, 10 => ConsoleColor.DarkRed, _ => ConsoleColor.White
        };
    }

    private static void DisplayFrequencyBars(VisualizationFrame f, int termWidth)
    {
        int barHeight = Math.Max(10, Math.Min(30, f.TerminalHeight - 15));
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

    private static void DisplayFrequencyLabels(VisualizationFrame f, int termWidth)
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
