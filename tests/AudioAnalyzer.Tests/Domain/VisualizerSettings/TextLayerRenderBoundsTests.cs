using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Domain.VisualizerSettings;

public sealed class TextLayerRenderBoundsTests
{
    [Fact]
    public void ToPixelRect_Null_IsFullViewport()
    {
        var (l, t, w, h) = TextLayerRenderBounds.ToPixelRect(null, 80, 24);
        Assert.Equal(0, l);
        Assert.Equal(0, t);
        Assert.Equal(80, w);
        Assert.Equal(24, h);
    }

    [Fact]
    public void ToPixelRect_FullNormalized_IsFullViewport()
    {
        var b = new TextLayerRenderBounds { X = 0, Y = 0, Width = 1, Height = 1 };
        var (l, t, pw, ph) = TextLayerRenderBounds.ToPixelRect(b, 80, 24);
        Assert.Equal(0, l);
        Assert.Equal(0, t);
        Assert.Equal(80, pw);
        Assert.Equal(24, ph);
    }

    [Fact]
    public void ToPixelRect_BottomHalf()
    {
        var b = new TextLayerRenderBounds { X = 0, Y = 0.5, Width = 1, Height = 0.5 };
        var (l, t, pw, ph) = TextLayerRenderBounds.ToPixelRect(b, 80, 24);
        Assert.Equal(0, l);
        Assert.Equal(12, t);
        Assert.Equal(80, pw);
        Assert.Equal(12, ph);
    }

    [Fact]
    public void ToPixelRect_MinimumOneCell()
    {
        var b = new TextLayerRenderBounds { X = 0.5, Y = 0.5, Width = 0.01, Height = 0.01 };
        var (_, _, pw, ph) = TextLayerRenderBounds.ToPixelRect(b, 100, 100);
        Assert.True(pw >= 1);
        Assert.True(ph >= 1);
    }

    [Fact]
    public void DeepCopy_IsIndependent()
    {
        var a = new TextLayerRenderBounds { X = 0.1, Y = 0.2, Width = 0.3, Height = 0.4 };
        var b = a.DeepCopy();
        a.X = 0.9;
        Assert.Equal(0.1, b.X, precision: 10);
    }
}
