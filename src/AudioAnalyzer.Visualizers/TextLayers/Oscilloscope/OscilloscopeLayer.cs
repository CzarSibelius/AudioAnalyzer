using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Renders the time-domain waveform as a layer (oscilloscope trace).</summary>
public sealed class OscilloscopeLayer : ITextLayerRenderer
{
    public TextLayerType LayerType => TextLayerType.Oscilloscope;

    public (double Offset, int SnippetIndex) Draw(
        TextLayerSettings layer,
        ref (double Offset, int SnippetIndex) state,
        TextLayerDrawContext ctx)
    {
        int w = ctx.Width;
        int h = ctx.Height;
        var snapshot = ctx.Snapshot;

        if (snapshot.Waveform is not { Length: > 0 } || snapshot.WaveformSize <= 0)
        {
            return state;
        }

        int width = Math.Min(w, snapshot.WaveformSize);
        int centerY = h / 2;
        var s = layer.GetCustom<OscilloscopeSettings>() ?? new OscilloscopeSettings();
        double gain = Math.Clamp(s.Gain > 0 ? s.Gain : 2.5, 1.0, 10.0);
        int step = Math.Max(1, snapshot.WaveformSize / width);
        int prevY = centerY;

        for (int x = 0; x < width; x++)
        {
            int sampleIndex = (snapshot.WaveformPosition + x * step) % snapshot.WaveformSize;
            float sample = snapshot.Waveform[sampleIndex];
            float scaled = Math.Clamp(sample * (float)gain, -1f, 1f);
            int y = centerY - (int)(scaled * (h / 2 - 1));
            y = Math.Clamp(y, 0, h - 1);
            int minY = Math.Min(prevY, y);
            int maxY = Math.Max(prevY, y);
            for (int lineY = minY; lineY <= maxY; lineY++)
            {
                var color = GetColorFromPalette(lineY, centerY, h, ctx.Palette);
                ctx.Buffer.Set(x, lineY, '█', color);
            }
            prevY = y;
        }

        return state;
    }

    private static PaletteColor GetColorFromPalette(int y, int centerY, int height, IReadOnlyList<PaletteColor>? palette)
    {
        if (palette is { Count: > 0 })
        {
            double distance = (height / 2 <= 0) ? 0 : Math.Abs(y - centerY) / (double)(height / 2);
            int idx = Math.Min((int)(distance * palette.Count), Math.Max(0, palette.Count - 1));
            return palette[idx];
        }
        return GetOscilloscopeColor(y, centerY, height);
    }

    private static PaletteColor GetOscilloscopeColor(int y, int centerY, int height)
    {
        if (height / 2 <= 0)
        {
            return PaletteColor.FromConsoleColor(ConsoleColor.Cyan);
        }
        double distance = Math.Abs(y - centerY) / (double)(height / 2);
        var cc = distance switch
        {
            >= 0.8 => ConsoleColor.Red,
            >= 0.6 => ConsoleColor.Yellow,
            >= 0.4 => ConsoleColor.Green,
            _ => ConsoleColor.Cyan
        };
        return PaletteColor.FromConsoleColor(cc);
    }
}
