using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.FractalZoom;

/// <summary>Tests escape-time helpers used by <see cref="FractalZoomLayer"/>.</summary>
public sealed class FractalZoomSamplerTests
{
    [Fact]
    public void Mandelbrot_origin_inside_returns_max_iterations()
    {
        int n = FractalZoomSampler.EscapeIterationsMandelbrot(0, 0, 64);
        Assert.Equal(64, n);
    }

    [Fact]
    public void Mandelbrot_far_out_escapes_quickly()
    {
        int n = FractalZoomSampler.EscapeIterationsMandelbrot(2, 0, 64);
        Assert.True(n < 10);
    }

    [Fact]
    public void Julia_with_escape_at_origin_matches_expected()
    {
        // c = 2+0i Julia: z0=0 -> z1=2 -> escape
        int n = FractalZoomSampler.EscapeIterationsJulia(0, 0, 2, 0, 32);
        Assert.Equal(2, n);
    }

    [Fact]
    public void Julia_classic_constant_zero_start_stays_bounded_for_many_steps()
    {
        int n = FractalZoomSampler.EscapeIterationsJulia(0, 0, -0.8, 0.156, 128);
        Assert.Equal(128, n);
    }

    [Fact]
    public void Smooth_mandelbrot_origin_inside_returns_max_iterations()
    {
        double n = FractalZoomSampler.EscapeSmoothMandelbrot(0, 0, 64);
        Assert.Equal(64, n);
    }

    [Fact]
    public void Smooth_mandelbrot_far_out_is_fractional_and_small()
    {
        double n = FractalZoomSampler.EscapeSmoothMandelbrot(2, 0, 64);
        Assert.True(n < 10);
        Assert.True(n >= 0);
    }

    [Fact]
    public void Smooth_julia_escape_at_origin_matches_order_of_integer()
    {
        double smooth = FractalZoomSampler.EscapeSmoothJulia(0, 0, 2, 0, 32);
        int integer = FractalZoomSampler.EscapeIterationsJulia(0, 0, 2, 0, 32);
        Assert.Equal(2, integer);
        Assert.InRange(smooth, 1.0, 3.0);
    }
}
