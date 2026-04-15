using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

/// <summary>Overview column→bucket mapping and non-filled connector policy (ADR-0077 strip).</summary>
public sealed class WaveformStripLayerOverviewMappingTests
{
    [Theory]
    [InlineData(0, 120, 8192)]
    [InlineData(119, 120, 8192)]
    [InlineData(0, 2, 100)]
    [InlineData(1, 2, 100)]
    public void OverviewBucketIndexForColumn_matches_partition_of_chronological_resample(int x, int w, int useLen)
    {
        const int validSamples = 100_000;
        var vis = new WaveformStripVisibleChronology(validSamples, validSamples, 0);
        int ch = w < 2 ? validSamples - 1 : x * (validSamples - 1) / (w - 1);
        int expected = WaveformOverviewBucketIndex.FromChronologicalIndex(ch, validSamples, useLen);
        int bi = WaveformStripLayer.OverviewBucketIndexForColumn(x, w, useLen, vis);
        Assert.Equal(expected, bi);
    }

    [Fact]
    public void OverviewBucketIndexForColumn_single_column_is_newest_bucket()
    {
        const int validSamples = 10_000;
        var vis = new WaveformStripVisibleChronology(validSamples, validSamples, 0);
        int expected = WaveformOverviewBucketIndex.FromChronologicalIndex(validSamples - 1, validSamples, 100);
        Assert.Equal(expected, WaveformStripLayer.OverviewBucketIndexForColumn(0, 1, 100, vis));
    }

    [Fact]
    public void Overview_nonfilled_wide_bucket_steps_yield_one_block_per_column_not_long_bridges()
    {
        const int buckets = 32;
        var min = new float[buckets];
        var max = new float[buckets];
        for (int i = 0; i < buckets; i++)
        {
            float v = (i % 2 == 0) ? 0.95f : -0.95f;
            min[i] = max[i] = v;
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

        const int w = 5;
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(w, 14);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var palette = new List<PaletteColor>
        {
            PaletteColor.FromConsoleColor(ConsoleColor.Cyan)
        };

        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        layer.SetCustom(new WaveformStripSettings
        {
            Gain = 2.5,
            Filled = false,
            ColorMode = WaveformStripColorMode.PaletteDistance,
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
            ViewportWidth = w,
            ViewportHeight = 14,
            Width = w,
            Height = 14,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var strip = new WaveformStripLayer();
        var state = (0.0, 0);
        strip.Draw(layer, ref state, ctx);

        const int bandTop = 0;
        const int bandHeight = 14;

        int blockCount = 0;
        int maxRunInColumn = 0;
        for (int col = 0; col < w; col++)
        {
            int run = 0;
            for (int row = bandTop; row < bandTop + bandHeight; row++)
            {
                var (ch, _) = buffer.Get(col, row);
                if (ch == '█')
                {
                    blockCount++;
                    run++;
                    if (run > maxRunInColumn)
                    {
                        maxRunInColumn = run;
                    }
                }
                else
                {
                    run = 0;
                }
            }
        }

        Assert.Equal(w, blockCount);
        Assert.Equal(1, maxRunInColumn);
    }

    [Fact]
    public void Overview_nonfilled_connects_vertical_segments_when_consecutive_columns_are_adjacent_buckets()
    {
        const int buckets = 4;
        var min = new float[buckets];
        var max = new float[buckets];
        for (int i = 0; i < buckets; i++)
        {
            float v = (i % 2 == 0) ? 0.9f : -0.9f;
            min[i] = max[i] = v;
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
            WaveformOverviewSpanSeconds = 0.1,
            WaveformOverviewValidSampleCount = buckets * 100,
            WaveformOverviewBuiltValidSampleCount = buckets * 100,
            WaveformOverviewBuiltNewestMonoSampleIndex = buckets * 100,
            WaveformOverviewBuiltOldestMonoSampleIndex = 1,
            CurrentBpm = 0
        };

        const int w = 4;
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(w, 14);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Cyan) };

        var layer = new TextLayerSettings { LayerType = TextLayerType.WaveformStrip };
        layer.SetCustom(new WaveformStripSettings
        {
            Gain = 2.5,
            Filled = false,
            ColorMode = WaveformStripColorMode.PaletteDistance,
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
            ViewportWidth = w,
            ViewportHeight = 14,
            Width = w,
            Height = 14,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var strip = new WaveformStripLayer();
        var state = (0.0, 0);
        strip.Draw(layer, ref state, ctx);

        const int bandTop = 0;
        const int bandHeight = 14;

        int blockCount = 0;
        for (int col = 0; col < w; col++)
        {
            for (int row = bandTop; row < bandTop + bandHeight; row++)
            {
                var (ch, _) = buffer.Get(col, row);
                if (ch == '█')
                {
                    blockCount++;
                }
            }
        }

        Assert.True(blockCount > w);
    }
}
