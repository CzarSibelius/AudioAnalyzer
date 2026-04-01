namespace AudioAnalyzer.Visualizers;

/// <summary>Controls phase-to-zoom mapping for <see cref="FractalZoomLayer"/>.</summary>
public enum FractalZoomDwell
{
    /// <summary>Scale follows phase 1:1 (original behavior).</summary>
    Linear,

    /// <summary>Piecewise linear: slower scale change through the middle ~70% of phase.</summary>
    Mild,

    /// <summary>Stronger plateau: middle ~80% of phase maps through a narrower scale band more slowly.</summary>
    Strong
}
