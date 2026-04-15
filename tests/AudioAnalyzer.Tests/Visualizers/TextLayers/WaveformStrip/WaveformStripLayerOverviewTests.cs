using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

/// <summary>Strip draws from decimated overview when present (ADR-0077).</summary>
public sealed class WaveformStripLayerOverviewTests
{
    [Fact]
    public void Overview_min_max_draws_blocks_without_legacy_waveform()
    {
        const int buckets = 32;
        var min = new float[buckets];
        var max = new float[buckets];
        for (int i = 0; i < buckets; i++)
        {
            min[i] = -0.95f;
            max[i] = 0.95f;
        }

        var lo = new float[buckets];
        var mid = new float[buckets];
        var hi = new float[buckets];
        Array.Fill(lo, 0.1f);
        Array.Fill(mid, 0.2f);
        Array.Fill(hi, 0.05f);

        var analysis = new AudioAnalysisSnapshot
        {
            Waveform = Array.Empty<float>(),
            WaveformSize = 0,
            WaveformPosition = 0,
            WaveformOverviewMin = min,
            WaveformOverviewMax = max,
            WaveformOverviewBandLow = lo,
            WaveformOverviewBandMid = mid,
            WaveformOverviewBandHigh = hi,
            WaveformOverviewLength = buckets,
            WaveformOverviewSpanSeconds = 1.0,
            WaveformOverviewValidSampleCount = buckets * 100,
            WaveformOverviewBuiltValidSampleCount = buckets * 100,
            WaveformOverviewBuiltNewestMonoSampleIndex = buckets * 100,
            WaveformOverviewBuiltOldestMonoSampleIndex = 1,
            CurrentBpm = 0
        };

        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(40, 14);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var palette = new List<PaletteColor>
        {
            PaletteColor.FromConsoleColor(ConsoleColor.Cyan),
            PaletteColor.FromConsoleColor(ConsoleColor.Green)
        };

        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        layer.SetCustom(new WaveformStripSettings
        {
            Gain = 4.0,
            Filled = true,
            ColorMode = WaveformStripColorMode.SpectralApprox,
            ShowBeatGrid = false,
            ShowBeatMarkers = false,
            ShowTimeLabel = false
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 40,
            ViewportHeight = 14,
            Width = 40,
            Height = 14,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var strip = new WaveformStripLayer();
        var state = (0.0, 0);
        strip.Draw(layer, ref state, ctx);

        int blockCount = 0;
        for (int row = 0; row < 14; row++)
        {
            for (int col = 0; col < 40; col++)
            {
                var (ch, _) = buffer.Get(col, row);
                if (ch == '█')
                {
                    blockCount++;
                }
            }
        }

        Assert.True(blockCount > 50);
    }
}
