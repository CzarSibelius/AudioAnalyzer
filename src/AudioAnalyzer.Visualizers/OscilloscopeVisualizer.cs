using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class OscilloscopeVisualizer : IVisualizer
{
    public void Render(AnalysisSnapshot snapshot, IDisplayDimensions dimensions, int displayStartRow)
    {
        int termWidth = dimensions.Width;
        int termHeight = dimensions.Height;
        if (termWidth < 30 || termHeight < 15) return;

        Console.SetCursorPosition(0, displayStartRow);
        int height = Math.Max(10, Math.Min(25, termHeight - 12));
        int width = Math.Min(termWidth - 4, snapshot.WaveformSize);
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
        int step = Math.Max(1, snapshot.WaveformSize / width);
        int prevY = centerY;
        for (int x = 0; x < width; x++)
        {
            int sampleIndex = (snapshot.WaveformPosition + x * step) % snapshot.WaveformSize;
            float sample = snapshot.Waveform[sampleIndex];
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
}
