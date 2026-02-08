using System.Text;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

public sealed class OscilloscopeVisualizer : IVisualizer
{
    private readonly StringBuilder _lineBuffer = new(256);

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        if (viewport.Width < 30 || viewport.MaxLines < 5) return;

        int maxHeight = Math.Max(1, viewport.MaxLines - 4);
        int height = Math.Max(10, Math.Min(25, maxHeight));
        int width = Math.Min(viewport.Width - 4, snapshot.WaveformSize);
        int centerY = height / 2;
        int expectedLines = 1 + height + 1 + 1; // top + grid + bottom + footer (one line)

        Console.SetCursorPosition(0, viewport.StartRow);
        Console.WriteLine(AnsiConsole.ToAnsiString("  ┌" + new string('─', width) + "┐", ConsoleColor.DarkGray));

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
            float gain = (float)Math.Clamp(snapshot.OscilloscopeGain, 1.0, 10.0);
            float scaled = Math.Clamp(sample * gain, -1f, 1f);
            int y = centerY - (int)(scaled * (height / 2 - 1));
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
            _lineBuffer.Clear();
            AnsiConsole.AppendColored(_lineBuffer, "  │", ConsoleColor.DarkGray);
            for (int x = 0; x < width; x++)
                AnsiConsole.AppendColored(_lineBuffer, screen[y, x], colors[y, x]);
            AnsiConsole.AppendColored(_lineBuffer, "│", ConsoleColor.DarkGray);
            Console.WriteLine(_lineBuffer.ToString());
        }

        Console.WriteLine(AnsiConsole.ToAnsiString("  └" + new string('─', width) + "┘", ConsoleColor.DarkGray));
        Console.WriteLine(VisualizerViewport.TruncateToWidth("  Waveform Display - Shows audio amplitude over time".PadRight(viewport.Width), viewport.Width));
    }

    private static ConsoleColor GetOscilloscopeColor(int y, int centerY, int height)
    {
        double distance = Math.Abs(y - centerY) / (double)(height / 2);
        return distance switch { >= 0.8 => ConsoleColor.Red, >= 0.6 => ConsoleColor.Yellow, >= 0.4 => ConsoleColor.Green, _ => ConsoleColor.Cyan };
    }
}
