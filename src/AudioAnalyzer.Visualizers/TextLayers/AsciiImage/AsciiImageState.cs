using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer state for the ASCII image layer: scroll position, zoom phase, and cached frame.</summary>
public sealed class AsciiImageState
{
    public double ScrollX { get; set; }
    public double ScrollY { get; set; }
    public double ZoomPhase { get; set; }

    /// <summary>Cached ASCII frame; invalidated when image path, dimensions, or palette source change.</summary>
    public AsciiFrame? CachedFrame { get; set; }
    public string? CachedPath { get; set; }
    public int CachedWidth { get; set; }
    public int CachedHeight { get; set; }
    public AsciiImagePaletteSource CachedPaletteSource { get; set; }
}
