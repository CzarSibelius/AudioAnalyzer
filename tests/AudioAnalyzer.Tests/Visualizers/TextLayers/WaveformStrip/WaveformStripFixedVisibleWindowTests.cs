using AudioAnalyzer.Application;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

public sealed class WaveformStripFixedVisibleWindowTests
{
    [Fact]
    public void OverviewBucketIndex_maps_fixed_tail_columns_to_ring_end()
    {
        const int n = 100_000;
        const int useLen = 8192;
        const int w = 12;
        const int window = 10_000;
        int oldestVis = n - window;
        var vis = new WaveformStripVisibleChronology(n, window, oldestVis);

        int bLeft = WaveformStripLayer.OverviewBucketIndexForColumn(0, w, useLen, vis);
        int bRight = WaveformStripLayer.OverviewBucketIndexForColumn(w - 1, w, useLen, vis);

        int expectLeft = WaveformOverviewBucketIndex.FromChronologicalIndex(oldestVis, n, useLen);
        int expectRight = WaveformOverviewBucketIndex.FromChronologicalIndex(n - 1, n, useLen);

        Assert.Equal(expectLeft, bLeft);
        Assert.Equal(expectRight, bRight);
    }

    [Fact]
    public void Full_history_vis_matches_linear_chronology_across_columns()
    {
        const int n = 50_000;
        const int useLen = 100;
        const int w = 40;
        var vis = new WaveformStripVisibleChronology(n, n, 0);
        for (int x = 0; x < w; x++)
        {
            int ch = x * (n - 1) / (w - 1);
            int expected = WaveformOverviewBucketIndex.FromChronologicalIndex(ch, n, useLen);
            Assert.Equal(expected, WaveformStripLayer.OverviewBucketIndexForColumn(x, w, useLen, vis));
        }
    }
}
