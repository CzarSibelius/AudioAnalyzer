using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.FractalZoom;

public sealed class FractalZoomIllusoryReseedTests
{
    [Fact]
    public void AnchorForSegment_0_matches_legacy_default()
    {
        (double re, double im) = FractalZoomIllusoryReseed.AnchorForSegment(0);
        Assert.Equal(-0.75, re);
        Assert.Equal(0.05, im);
    }

    [Fact]
    public void JuliaNudgeForSegment_0_is_zero()
    {
        (double jr, double ji) = FractalZoomIllusoryReseed.JuliaNudgeForSegment(0);
        Assert.Equal(0.0, jr);
        Assert.Equal(0.0, ji);
    }

    [Fact]
    public void JuliaNudgeForSegment_positive_is_bounded()
    {
        for (int s = 1; s < 40; s++)
        {
            (double jr, double ji) = FractalZoomIllusoryReseed.JuliaNudgeForSegment(s);
            Assert.InRange(jr, -0.06, 0.06);
            Assert.InRange(ji, -0.06, 0.06);
        }
    }

    [Fact]
    public void Consecutive_segment_anchors_differ_by_at_least_contract_epsilon()
    {
        const double minStep = 1e-4;
        for (int s = 0; s < 500; s++)
        {
            double d = FractalZoomIllusoryReseed.AnchorStepDistance(s);
            Assert.True(d >= minStep, $"segment {s}: step distance {d} < {minStep}");
        }
    }
}
