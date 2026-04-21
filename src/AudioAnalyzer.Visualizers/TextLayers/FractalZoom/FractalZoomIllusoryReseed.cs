namespace AudioAnalyzer.Visualizers;

/// <summary>Deterministic orbit anchors and Julia nudges for <see cref="FractalZoomLayer"/> illusory infinite zoom (not a continuous complex-plane path).</summary>
public static class FractalZoomIllusoryReseed
{
    /// <summary>Classic default at index 0; further indices jump to other recognizable regions.</summary>
    private static readonly (double Re, double Im)[] Anchors =
    {
        (-0.75, 0.05),
        (0.285, 0.01),
        (-0.5, 0.6),
        (-1.25, 0.0),
        (-0.16, 1.0405),
        (-0.8, 0.156),
        (0.35, 0.35),
        (-1.768, 0.0),
        (-0.125, 0.743),
        (-0.7269, 0.1889),
        (-0.2, 0.8),
        (0.00164, -0.82247),
        (-0.4, -0.55),
        (0.15, -0.65),
        (-1.45, 0.045),
        (-0.55, -0.5),
        (0.45, 0.42),
        (-0.95, 0.25),
    };

    /// <summary>Orbit center base for the given segment (non-negative).</summary>
    public static (double Re, double Im) AnchorForSegment(int segment)
    {
        if (segment < 0)
        {
            segment = 0;
        }

        int i = segment % Anchors.Length;
        return Anchors[i];
    }

    /// <summary>Small offsets added to preset Julia constants; zero for segment 0.</summary>
    public static (double OffsetRe, double OffsetIm) JuliaNudgeForSegment(int segment)
    {
        if (segment <= 0)
        {
            return (0.0, 0.0);
        }

        double t = segment * 2.399963229728653;
        double djr = 0.055 * Math.Sin(t);
        double dji = 0.055 * Math.Cos(t * 0.73);
        return (Math.Clamp(djr, -0.06, 0.06), Math.Clamp(dji, -0.06, 0.06));
    }

    /// <summary>Euclidean distance between anchors for consecutive segments (modulo catalog).</summary>
    public static double AnchorStepDistance(int segment)
    {
        (double aRe, double aIm) = AnchorForSegment(segment);
        (double bRe, double bIm) = AnchorForSegment(segment + 1);
        double dr = bRe - aRe;
        double di = bIm - aIm;
        return Math.Sqrt((dr * dr) + (di * di));
    }
}
