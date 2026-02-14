using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>
/// Renders stacked waveform snapshots (Unknown Pleasures style). Bottom line is realtime;
/// others are beat-triggered frozen snapshots.
/// </summary>
public sealed class UnknownPleasuresLayer : ITextLayerRenderer
{
    private const int SnapshotWidth = 120;
    private const int MaxSnapshots = 14;

    /// <summary>ASCII gradient from light to heavy (space, dot, comma, hyphen, quote, etc.).</summary>
    private static readonly string AsciiGradient = " .,'-_\"/~*#";

    public TextLayerType LayerType => TextLayerType.UnknownPleasures;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;

        if (w < 20 || h < 5)
        {
            return state;
        }

        var palette = ctx.Palette;
        if (palette is null || palette.Count == 0)
        {
            return state;
        }

        var snapshot = ctx.Snapshot;
        var magnitudes = snapshot.SmoothedMagnitudes ?? Array.Empty<double>();
        int numBands = Math.Min(snapshot.NumBands, magnitudes.Length);
        if (numBands == 0)
        {
            return state;
        }

        double gain = snapshot.TargetMaxMagnitude > 0.0001 ? Math.Min(1000, 1.0 / snapshot.TargetMaxMagnitude) : 1000;
        var upState = ctx.UnknownPleasuresStateForLayer;

        // Take a snapshot only on beat (or first frame so something is shown)
        bool onBeat = snapshot.BeatCount > upState.LastBeatCount;
        bool firstFrame = upState.Snapshots.Count == 0;
        if (onBeat || firstFrame)
        {
            upState.LastBeatCount = snapshot.BeatCount;
            var current = new double[SnapshotWidth];
            BuildPulseFromMagnitudes(magnitudes, numBands, gain, current);
            upState.Snapshots.Add(current);
            while (upState.Snapshots.Count > MaxSnapshots)
            {
                upState.Snapshots.RemoveAt(0);
            }
        }

        BuildPulseFromMagnitudes(magnitudes, numBands, gain, upState.LivePulse);

        int barWidth = w;
        int numSnapshots = upState.Snapshots.Count;
        int maxRows = h - 1;
        const int linesPerSnapshot = 3;
        const int gapRows = 1;
        int rowsPerSnapshot = linesPerSnapshot + gapRows;
        int canShow = (maxRows + 1) / rowsPerSnapshot;
        int startIndex = Math.Max(0, numSnapshots - canShow);

        if (onBeat)
        {
            upState.ColorOffset = (upState.ColorOffset + 1) % Math.Max(1, palette.Count);
        }

        int rowIndex = 0;
        for (int j = 0; j < canShow && rowIndex < h; j++)
        {
            int i = startIndex + j;
            if (i >= numSnapshots)
            {
                break;
            }

            double[] pulse = (i == numSnapshots - 1) ? upState.LivePulse : upState.Snapshots[i];
            var color = palette[(i + upState.ColorOffset) % palette.Count];

            for (int line = 0; line < linesPerSnapshot && rowIndex < h; line++)
            {
                for (int c = 0; c < barWidth; c++)
                {
                    int src = (c * SnapshotWidth) / barWidth;
                    if (src >= pulse.Length)
                    {
                        src = pulse.Length - 1;
                    }

                    double mag = pulse[src];
                    char ch = GetCharForMagnitude(mag, line);
                    ctx.Buffer.Set(c, rowIndex, ch, color);
                }
                rowIndex++;
            }

            if (rowIndex >= h)
            {
                break;
            }

            if (j < canShow - 1)
            {
                rowIndex++;
            }
        }

        return state;
    }

    private static void BuildPulseFromMagnitudes(double[] magnitudes, int numBands, double gain, double[] buffer)
    {
        for (int c = 0; c < SnapshotWidth; c++)
        {
            int band = (c * numBands) / SnapshotWidth;
            if (band >= magnitudes.Length)
            {
                band = magnitudes.Length - 1;
            }

            buffer[c] = Math.Min(magnitudes[band] * gain * 0.8, 1.0);
        }

        double min = buffer.Min(), max = buffer.Max();
        double range = max - min;
        if (range > 0.0001)
        {
            for (int c = 0; c < SnapshotWidth; c++)
            {
                buffer[c] = (buffer[c] - min) / range;
            }
        }
    }

    private static char GetCharForMagnitude(double mag, int line)
    {
        if (line == 0)
        {
            if (mag >= 2.0 / 3.0)
            {
                return AsciiGradient[Math.Min(1 + (int)((mag - 2.0 / 3.0) * 3 * (AsciiGradient.Length - 2)), AsciiGradient.Length - 1)];
            }
            return ' ';
        }

        if (line == 1)
        {
            if (mag >= 1.0 / 3.0 && mag < 2.0 / 3.0)
            {
                return AsciiGradient[Math.Min(1 + (int)((mag - 1.0 / 3.0) * 1.5 * (AsciiGradient.Length - 2)), AsciiGradient.Length - 1)];
            }
            return ' ';
        }

        // line == 2
        if (mag >= 1.0 / 3.0)
        {
            return ' ';
        }

        int idx = 1 + (int)(mag * (AsciiGradient.Length - 2));
        idx = Math.Clamp(idx, 1, AsciiGradient.Length - 1);
        return AsciiGradient[idx];
    }
}
