using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.FractalZoom;

/// <summary>Tests <see cref="FractalZoomAnimation.RemapPhaseToScaleT"/>.</summary>
public sealed class FractalZoomAnimationTests
{
    [Fact]
    public void Linear_passthrough()
    {
        Assert.Equal(0.0, FractalZoomAnimation.RemapPhaseToScaleT(0, FractalZoomDwell.Linear));
        Assert.Equal(0.5, FractalZoomAnimation.RemapPhaseToScaleT(0.5, FractalZoomDwell.Linear));
        Assert.Equal(1.0, FractalZoomAnimation.RemapPhaseToScaleT(1.0, FractalZoomDwell.Linear));
    }

    [Fact]
    public void Mild_and_Strong_preserve_endpoints()
    {
        Assert.Equal(0.0, FractalZoomAnimation.RemapPhaseToScaleT(0, FractalZoomDwell.Mild));
        Assert.Equal(0.0, FractalZoomAnimation.RemapPhaseToScaleT(0, FractalZoomDwell.Strong));
        Assert.Equal(1.0, FractalZoomAnimation.RemapPhaseToScaleT(1.0, FractalZoomDwell.Mild));
        Assert.Equal(1.0, FractalZoomAnimation.RemapPhaseToScaleT(1.0, FractalZoomDwell.Strong));
    }

    [Fact]
    public void Mild_and_Strong_monotonic_in_phase()
    {
        double prevM = -1;
        double prevS = -1;
        for (int i = 0; i <= 20; i++)
        {
            double p = i / 20.0;
            double m = FractalZoomAnimation.RemapPhaseToScaleT(p, FractalZoomDwell.Mild);
            double s = FractalZoomAnimation.RemapPhaseToScaleT(p, FractalZoomDwell.Strong);
            Assert.True(m >= prevM - 1e-9);
            Assert.True(s >= prevS - 1e-9);
            prevM = m;
            prevS = s;
        }
    }
}
