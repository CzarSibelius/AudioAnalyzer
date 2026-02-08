using System.Text;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Unknown Pleasures visualizer: multiple stacked waveform snapshots with the most recent
/// at the bottom, gaps between each pulse (like the pulsar plot).
/// </summary>
public sealed class UnknownPleasuresVisualizer : IVisualizer
{
    private const int SnapshotWidth = 120;
    private const int MaxSnapshots = 14;

    /// <summary>ASCII gradient from light to heavy (space, dot, comma, hyphen, quote, etc.).</summary>
    private static readonly string AsciiGradient = " .,'-_\"/~*#";

    private readonly StringBuilder _lineBuffer = new(2048);
    private readonly List<double[]> _snapshots = new(MaxSnapshots);
    private int _lastBeatCount = -1;
    private int _colorOffset;

    public void Render(AnalysisSnapshot snapshot, VisualizerViewport viewport)
    {
        if (viewport.Width < 20 || viewport.MaxLines < 5) return;

        var palette = snapshot.UnknownPleasuresPalette;
        if (palette is null || palette.Count == 0)
            return;

        var magnitudes = snapshot.SmoothedMagnitudes ?? Array.Empty<double>();
        int numBands = Math.Min(snapshot.NumBands, magnitudes.Length);
        if (numBands == 0) return;

        double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;

        // Take a snapshot only on beat (or first frame so something is shown)
        bool onBeat = snapshot.BeatCount > _lastBeatCount;
        bool firstFrame = _snapshots.Count == 0;
        if (onBeat || firstFrame)
        {
            _lastBeatCount = snapshot.BeatCount;
            var current = new double[SnapshotWidth];
            for (int c = 0; c < SnapshotWidth; c++)
            {
                int band = (c * numBands) / SnapshotWidth;
                if (band >= magnitudes.Length) band = magnitudes.Length - 1;
                current[c] = Math.Min(magnitudes[band] * gain * 0.8, 1.0);
            }
            double min = current.Min(), max = current.Max();
            double range = max - min;
            if (range > 0.0001)
            {
                for (int c = 0; c < SnapshotWidth; c++)
                    current[c] = (current[c] - min) / range;
            }
            _snapshots.Add(current);
            while (_snapshots.Count > MaxSnapshots)
                _snapshots.RemoveAt(0);
        }

        int barWidth = viewport.Width;
        int numSnapshots = _snapshots.Count;
        int maxRows = viewport.MaxLines - 1;
        const int linesPerSnapshot = 3;
        const int gapRows = 1;
        int rowsPerSnapshot = linesPerSnapshot + gapRows;
        int canShow = (maxRows + 1) / rowsPerSnapshot;
        int startIndex = Math.Max(0, numSnapshots - canShow);

        if (onBeat)
            _colorOffset = (_colorOffset + 1) % Math.Max(1, palette.Count);

        Console.SetCursorPosition(0, viewport.StartRow);

        int rowIndex = 0;
        for (int j = 0; j < canShow && rowIndex < viewport.MaxLines; j++)
        {
            int i = startIndex + j;
            if (i >= numSnapshots) break;
            double[] pulse = _snapshots[i];
            PaletteColor color = palette[(i + _colorOffset) % palette.Count];

            for (int line = 0; line < linesPerSnapshot && rowIndex < viewport.MaxLines; line++)
            {
                _lineBuffer.Clear();
                for (int c = 0; c < barWidth; c++)
                {
                    int src = (c * SnapshotWidth) / barWidth;
                    if (src >= pulse.Length) src = pulse.Length - 1;
                    double mag = pulse[src];
                    char ch;
                    if (line == 0)
                    {
                        if (mag >= 2.0 / 3.0)
                            ch = AsciiGradient[Math.Min(1 + (int)((mag - 2.0 / 3.0) * 3 * (AsciiGradient.Length - 2)), AsciiGradient.Length - 1)];
                        else
                            ch = ' ';
                    }
                    else if (line == 1)
                    {
                        if (mag >= 1.0 / 3.0 && mag < 2.0 / 3.0)
                            ch = AsciiGradient[Math.Min(1 + (int)((mag - 1.0 / 3.0) * 1.5 * (AsciiGradient.Length - 2)), AsciiGradient.Length - 1)];
                        else
                            ch = ' ';
                    }
                    else
                    {
                        if (mag >= 1.0 / 3.0)
                            ch = ' ';
                        else
                        {
                            int idx = 1 + (int)(mag * (AsciiGradient.Length - 2));
                            if (idx < 1) idx = 1;
                            if (idx >= AsciiGradient.Length) idx = AsciiGradient.Length - 1;
                            ch = AsciiGradient[idx];
                        }
                    }
                    if (ch != ' ')
                        AnsiConsole.AppendColored(_lineBuffer, ch, color);
                    else
                        _lineBuffer.Append(' ');
                }
                Console.WriteLine(_lineBuffer.ToString());
                rowIndex++;
            }

            if (rowIndex >= viewport.MaxLines) break;
            if (j < canShow - 1)
            {
                Console.WriteLine(new string(' ', barWidth));
                rowIndex++;
            }
        }
    }
}
