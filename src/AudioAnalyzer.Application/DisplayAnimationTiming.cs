namespace AudioAnalyzer.Application;

/// <summary>
/// Scale motion tuned for a 60 Hz reference so animation speed stays consistent when FPS varies (ADR-0072).
/// </summary>
public static class DisplayAnimationTiming
{
    /// <summary>Reference display rate used when tuning per-draw increments in layers and UI scrolling.</summary>
    public const double ReferenceHz = 60.0;

    /// <summary>
    /// Multiplier for increments defined as "per reference frame at <see cref="ReferenceHz"/>".
    /// At exactly 60 FPS, <paramref name="frameDeltaSeconds"/> is ~1/60 and this returns ~1.
    /// </summary>
    public static double ScaleForReference60(double frameDeltaSeconds) =>
        frameDeltaSeconds * ReferenceHz;
}
