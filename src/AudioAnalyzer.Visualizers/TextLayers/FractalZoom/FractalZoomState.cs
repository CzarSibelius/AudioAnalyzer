namespace AudioAnalyzer.Visualizers;

/// <summary>Animation state for FractalZoom: zoom phase, orbit around a default view, slow rotation.</summary>
public sealed class FractalZoomState
{
    /// <summary>0–1; wraps to create endless zoom cycles.</summary>
    public double ZoomPhase { get; set; }

    /// <summary>Advances when <see cref="ZoomPhase"/> wraps; drifts the view center.</summary>
    public double OrbitAngle { get; set; }

    /// <summary>Slow view rotation (radians).</summary>
    public double ViewRotation { get; set; }
}
