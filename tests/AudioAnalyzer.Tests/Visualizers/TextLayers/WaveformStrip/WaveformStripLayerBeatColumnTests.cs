using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

public sealed class WaveformStripLayerBeatColumnTests
{
    [Fact]
    public void ColumnForGlobalMonoSample_maps_newest_mark_to_right_column()
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
    }

    [Fact]
    public void BucketToColumnX_increases_with_bucket_index()
    {
        int w = 80;
        int useLen = 8192;
        int xLo = WaveformStripLayer.BucketToColumnX(100, w, useLen);
        int xHi = WaveformStripLayer.BucketToColumnX(8_000, w, useLen);
        Assert.True(xHi > xLo);
        Assert.InRange(xLo, 0, w - 1);
        Assert.InRange(xHi, 0, w - 1);
    }
}
