namespace AudioAnalyzer.Visualizers;

/// <summary>Which escape-time fractal to sample for <see cref="FractalZoomLayer"/>.</summary>
public enum FractalZoomMode
{
    /// <summary>Classic Mandelbrot set (c per pixel, z starts at 0).</summary>
    Mandelbrot,

    /// <summary>Julia set with fixed c from settings; z starts at the pixel.</summary>
    Julia
}
