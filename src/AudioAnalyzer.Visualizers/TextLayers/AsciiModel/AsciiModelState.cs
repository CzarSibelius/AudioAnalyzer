using System.Numerics;

namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer state for the ASCII 3D model layer: rotation, zoom phase, cached mesh, raster scratch, and cached folder listing.</summary>
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

    /// <summary>Resolved model folder path used for <see cref="CachedSortedObjPaths"/>; compared each frame to detect settings changes.</summary>
    public string? CachedResolvedObjFolder { get; set; }

    /// <summary>UTC last-write time of <see cref="CachedResolvedObjFolder"/> when paths were enumerated; <see langword="null"/> if time was unavailable (path-only invalidation until refreshed).</summary>
    public DateTime? CachedObjFolderLastWriteUtc { get; set; }

    /// <summary>Cached sorted full paths to <c>.obj</c> files under the effective model folder.</summary>
    public List<string>? CachedSortedObjPaths { get; set; }

    /// <summary>Reused z-buffer for <see cref="AsciiModelRenderMode.LegacyGradient"/>; length ≥ last draw width×height.</summary>
    public float[]? LegacyZBuffer { get; set; }

    /// <summary>Reused z-buffer for <see cref="AsciiModelRenderMode.Shape"/>; length ≥ last draw width×height×subsamples.</summary>
    public float[]? ShapeZBuffer { get; set; }

    /// <summary>Reused luminance buffer for shape mode; same length as <see cref="ShapeZBuffer"/>.</summary>
    public float[]? ShapeLumBuffer { get; set; }

    /// <summary>Reused world-space vertex positions after rotation; length ≥ last drawn mesh vertex count.</summary>
    public Vector3[]? WorldVertices { get; set; }

    /// <summary>Reused world-space vertex normals after rotation; same length as <see cref="WorldVertices"/>.</summary>
    public Vector3[]? WorldVertexNormals { get; set; }
}
