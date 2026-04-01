namespace AudioAnalyzer.Visualizers;

/// <summary>Escape-time iteration counts and smooth escape shading for Mandelbrot/Julia (cheap; for terminal preview, not deep-zoom math).</summary>
public static class FractalZoomSampler
{
    /// <summary>Mandelbrot: z₀=0, z←z²+c. Returns iteration count at escape or <paramref name="maxIter"/> if inside.</summary>
    public static int EscapeIterationsMandelbrot(double cr, double ci, int maxIter)
    {
        double zr = 0;
        double zi = 0;
        for (int i = 0; i < maxIter; i++)
        {
            if ((zr * zr + zi * zi) > 4.0)
            {
                return i;
            }

            double nzr = (zr * zr) - (zi * zi) + cr;
            double nzi = (2 * zr * zi) + ci;
            zr = nzr;
            zi = nzi;
        }

        return maxIter;
    }

    /// <summary>Julia: fixed c=(jr,ji), z starts at pixel (zr,zi).</summary>
    public static int EscapeIterationsJulia(double zr, double zi, double jr, double ji, int maxIter)
    {
        for (int i = 0; i < maxIter; i++)
        {
            if ((zr * zr + zi * zi) > 4.0)
            {
                return i;
            }

            double nzr = (zr * zr) - (zi * zi) + jr;
            double nzi = (2 * zr * zi) + ji;
            zr = nzr;
            zi = nzi;
        }

        return maxIter;
    }

    /// <summary>Smooth iteration count for Mandelbrot (same escape test as <see cref="EscapeIterationsMandelbrot"/>). Returns <paramref name="maxIter"/> if still inside after <paramref name="maxIter"/> steps.</summary>
    public static double EscapeSmoothMandelbrot(double cr, double ci, int maxIter)
    {
        double zr = 0;
        double zi = 0;
        for (int i = 0; i < maxIter; i++)
        {
            double r2 = (zr * zr) + (zi * zi);
            if (r2 > 4.0)
            {
                double zmag = Math.Sqrt(r2);
                double nu = i + 1.0 - Math.Log(Math.Log(zmag)) / Math.Log(2.0);
                if (nu < 0 || double.IsNaN(nu) || double.IsInfinity(nu))
                {
                    nu = i;
                }

                return Math.Clamp(nu, 0.0, maxIter);
            }

            double nzr = (zr * zr) - (zi * zi) + cr;
            double nzi = (2 * zr * zi) + ci;
            zr = nzr;
            zi = nzi;
        }

        return maxIter;
    }

    /// <summary>Smooth iteration count for Julia (same escape test as <see cref="EscapeIterationsJulia"/>).</summary>
    public static double EscapeSmoothJulia(double zr, double zi, double jr, double ji, int maxIter)
    {
        for (int i = 0; i < maxIter; i++)
        {
            double r2 = (zr * zr) + (zi * zi);
            if (r2 > 4.0)
            {
                double zmag = Math.Sqrt(r2);
                double nu = i + 1.0 - Math.Log(Math.Log(zmag)) / Math.Log(2.0);
                if (nu < 0 || double.IsNaN(nu) || double.IsInfinity(nu))
                {
                    nu = i;
                }

                return Math.Clamp(nu, 0.0, maxIter);
            }

            double nzr = (zr * zr) - (zi * zi) + jr;
            double nzi = (2 * zr * zi) + ji;
            zr = nzr;
            zi = nzi;
        }

        return maxIter;
    }
}
