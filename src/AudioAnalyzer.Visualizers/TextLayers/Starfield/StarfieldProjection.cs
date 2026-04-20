namespace AudioAnalyzer.Visualizers;

/// <summary>Perspective projection helpers for <see cref="StarfieldLayer"/> (see ADR-0082).</summary>
public static class StarfieldProjection
{
    /// <summary>Default vertical compression so circular star distributions look round in tall console cells.</summary>
    public const double DefaultCellAspect = 2.0;

    /// <summary>Projects model (X,Y,Z) to floating screen coordinates before rounding to cells.</summary>
    /// <param name="z">Depth; must be positive.</param>
    public static (double ScreenX, double ScreenY) Project(
        double x,
        double y,
        double z,
        double focalLength,
        double cellAspect,
        double centerX,
        double centerY)
    {
        double zz = z <= 1e-9 ? 1e-9 : z;
        double sx = centerX + focalLength * x / zz;
        double sy = centerY + focalLength * y / (zz * cellAspect);
        return (sx, sy);
    }
}
