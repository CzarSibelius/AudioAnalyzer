using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

/// <summary>Verifies <see cref="WaveformStripLayer"/> draws waveform cells into the buffer.</summary>
public sealed class WaveformStripLayerBasicTests
{
    [Fact]
    public void Nonzero_waveform_writes_block_chars_in_strip_band()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(24, 12);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var waveform = new float[64];
        for (int i = 0; i < waveform.Length; i++)
        {
            waveform[i] = (i % 2 == 0) ? 0.9f : -0.9f;
        }

        var analysis = new AudioAnalysisSnapshot
        {
            Waveform = waveform,
            WaveformSize = 64,
            WaveformPosition = 0
        };

        var palette = new List<PaletteColor>
        {
            PaletteColor.FromConsoleColor(ConsoleColor.Cyan),
            PaletteColor.FromConsoleColor(ConsoleColor.Green)
        };

        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        layer.SetCustom(new WaveformStripSettings { Gain = 5.0, Filled = false });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 24,
            ViewportHeight = 12,
            Width = 24,
            Height = 12,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var strip = new WaveformStripLayer();
        var state = (0.0, 0);
        strip.Draw(layer, ref state, ctx);

        int blockCount = 0;
        for (int row = 0; row < 12; row++)
        {
            for (int col = 0; col < 24; col++)
            {
                var (ch, _) = buffer.Get(col, row);
                if (ch == '█')
                {
                    blockCount++;
                }
            }
        }

        Assert.True(blockCount > 20);
    }
}
