namespace AudioAnalyzer.Visualizers;

/// <summary>Maps uniform <c>ZoomPhase</c> in <c>[0,1)</c> to scale parameter <c>t</c> in <c>[0,1]</c> for log-scale zoom. Plateau modes spend more frames in mid-zoom regimes where escape-time variation is visible on a coarse grid.</summary>
public static class FractalZoomAnimation
{
    /// <summary>Remaps phase to <c>t</c> used as <c>logS = logMin + t * (logMax - logMin)</c>. Monotonic in <paramref name="phase"/>; endpoints 0 and 1 preserved for <see cref="FractalZoomDwell.Linear"/> and plateau modes.</summary>
    public static double RemapPhaseToScaleT(double phase, FractalZoomDwell dwell)
    {
        phase = Math.Clamp(phase, 0.0, 1.0);
        if (dwell == FractalZoomDwell.Linear)
        {
            return phase;
        }

        // Piecewise linear: outer segments advance scale quickly (boring extremes); middle segment uses most phase for mid-scale detail.
        if (dwell == FractalZoomDwell.Mild)
        {
            const double p1 = 0.15;
            const double p2 = 0.85;
            const double t1 = 0.25;
            const double t2 = 0.75;
            return RemapThreeSegment(phase, p1, p2, t1, t2);
        }

        // Strong: wider middle phase span, slightly tighter t range so motion lingers in the interesting band.
        const double sp1 = 0.10;
        const double sp2 = 0.90;
        const double st1 = 0.20;
        const double st2 = 0.80;
        return RemapThreeSegment(phase, sp1, sp2, st1, st2);
    }

    private static double RemapThreeSegment(double phase, double p1, double p2, double t1, double t2)
    {
        if (phase <= p1)
        {
            return p1 <= 0 ? 0 : (phase / p1) * t1;
        }

        if (phase >= p2)
        {
            double denom = 1.0 - p2;
            return denom <= 0 ? 1.0 : t2 + ((phase - p2) / denom) * (1.0 - t2);
        }

        double mid = (phase - p1) / (p2 - p1);
        return t1 + mid * (t2 - t1);
    }
}
