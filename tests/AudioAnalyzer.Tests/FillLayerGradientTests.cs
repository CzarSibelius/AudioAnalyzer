using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Unit tests for Fill layer gradient parameterization.</summary>
public sealed class FillLayerGradientTests
{
    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(4, 1.0)]
    public void ComputeGradientT_LeftToRight_5x1(int x, double expected)
    {
        double t = FillLayer.ComputeGradientT(x, 0, 5, 1, FillGradientDirection.LeftToRight);
        Assert.Equal(expected, t, precision: 10);
    }

    [Fact]
    public void ComputeGradientT_TopLeftToBottomRight_corners()
    {
        double t00 = FillLayer.ComputeGradientT(0, 0, 5, 5, FillGradientDirection.TopLeftToBottomRight);
        double t44 = FillLayer.ComputeGradientT(4, 4, 5, 5, FillGradientDirection.TopLeftToBottomRight);
        Assert.Equal(0.0, t00, precision: 10);
        Assert.Equal(1.0, t44, precision: 10);
    }
}
