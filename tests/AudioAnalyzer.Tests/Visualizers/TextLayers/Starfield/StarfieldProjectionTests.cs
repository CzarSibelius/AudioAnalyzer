using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.Starfield;

/// <summary>Deterministic tests for <see cref="StarfieldProjection"/> (ADR-0082).</summary>
public sealed class StarfieldProjectionTests
{
    [Fact]
    public void Project_clamps_nonpositive_z_to_epsilon()
    {
        var (sx, sy) = StarfieldProjection.Project(1, 1, 0, 10, 2, 5, 5);
        Assert.True(double.IsFinite(sx) && double.IsFinite(sy));
    }

    [Fact]
    public void Project_center_matches_origin_at_zero_offset()
    {
        var (sx, sy) = StarfieldProjection.Project(0, 0, 10, 40, 2, 12, 8);
        Assert.Equal(12, sx, 9);
        Assert.Equal(8, sy, 9);
    }

    [Fact]
    public void Project_positive_x_shifts_screen_right()
    {
        double cx = 20;
        var (sx, _) = StarfieldProjection.Project(5, 0, 10, 40, 2, cx, 10);
        Assert.True(sx > cx);
    }

    [Fact]
    public void ClampStarCount_respects_hard_cap()
    {
        Assert.Equal(StarfieldLayer.MaxStarHardCap, StarfieldLayer.ClampStarCount(50_000));
        Assert.Equal(1, StarfieldLayer.ClampStarCount(0));
        Assert.Equal(42, StarfieldLayer.ClampStarCount(42));
    }
}
