namespace AudioAnalyzer.Domain;

/// <summary>How AsciiModel maps shading to characters.</summary>
public enum AsciiModelRenderMode
{
    /// <summary>Single sample per cell, face normal, gradient ramp (original).</summary>
    LegacyGradient = 0,

    /// <summary>Six staggered samples per cell, interpolated vertex normals, nearest shape-vector character.</summary>
    Shape = 1
}
