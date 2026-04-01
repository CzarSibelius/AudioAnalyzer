namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer state for the ASCII 3D model layer: rotation, zoom phase, and cached mesh.</summary>
public sealed class AsciiModelState
{
    /// <summary>Accumulated rotation angle (radians).</summary>
    public double RotationAngle { get; set; }

    /// <summary>Phase in [0,1) for zoom oscillation.</summary>
    public double ZoomPhase { get; set; }

    /// <summary>Cached mesh; invalidated when the source file path or content changes.</summary>
    public TriangleMesh? CachedMesh { get; set; }

    public string? CachedPath { get; set; }

    public long CachedFileLength { get; set; }

    public DateTime CachedLastWriteUtc { get; set; }
}
