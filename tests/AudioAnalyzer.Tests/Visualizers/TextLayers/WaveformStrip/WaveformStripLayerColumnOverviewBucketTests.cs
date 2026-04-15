using AudioAnalyzer.Application;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

public sealed class WaveformStripLayerColumnOverviewBucketTests
{
    [Theory]
    [InlineData(120, 100)]
    [InlineData(80, 8192)]
    [InlineData(2, 100)]
    public void ColumnForOverviewBucket_minimizes_bucket_distance_to_target(int w, int useLen)
    {
        const int validSamples = 50_000;
        var vis = new WaveformStripVisibleChronology(validSamples, validSamples, 0);
        for (int b = 0; b < useLen; b++)
        {
            int x = WaveformStripLayer.ColumnForOverviewBucket(b, w, useLen, vis);
            int bi = WaveformStripLayer.OverviewBucketIndexForColumn(x, w, useLen, vis);
            int dist = Math.Abs(bi - b);
            for (int xp = 0; xp < w; xp++)
            {
                int d = Math.Abs(WaveformStripLayer.OverviewBucketIndexForColumn(xp, w, useLen, vis) - b);
                Assert.True(d >= dist);
            }
        }
    }

    [Theory]
    [InlineData(120, 100)]
    [InlineData(4, 10)]
    public void ColumnForOverviewBucket_exact_when_some_column_maps_to_bucket(int w, int useLen)
    {
        const int validSamples = 20_000;
        var vis = new WaveformStripVisibleChronology(validSamples, validSamples, 0);
        for (int x = 0; x < w; x++)
        {
            int b = WaveformStripLayer.OverviewBucketIndexForColumn(x, w, useLen, vis);
            int x2 = WaveformStripLayer.ColumnForOverviewBucket(b, w, useLen, vis);
            Assert.Equal(WaveformStripLayer.OverviewBucketIndexForColumn(x2, w, useLen, vis), b);
        }
    }

    [Fact]
    public void ColumnForGlobalMonoSample_newest_and_oldest_hit_strip_endpoints_with_partition_mapping()
    {
        int w = 80;
        int useLen = 100;
        int validSamples = 1_000;
        long newest = 1_000;
        long oldest = 1;
        var vis = new WaveformStripVisibleChronology(validSamples, validSamples, 0);
        int xNewest = WaveformStripLayer.ColumnForGlobalMonoSample(newest, w, oldest, newest, useLen, vis);
        Assert.Equal(w - 1, xNewest);
        int xOldest = WaveformStripLayer.ColumnForGlobalMonoSample(oldest, w, oldest, newest, useLen, vis);
        Assert.Equal(0, xOldest);
        Assert.Equal(
            WaveformStripLayer.OverviewBucketIndexForColumn(xNewest, w, useLen, vis),
            WaveformOverviewBucketIndex.FromChronologicalIndex(validSamples - 1, validSamples, useLen));
    }
}
