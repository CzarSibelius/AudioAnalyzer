using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Per-layer cache for ASCII video: last converted frame and invalidation keys.</summary>
public sealed class AsciiVideoState
{
    /// <summary>Cached ASCII frame after resize/map.</summary>
    public AsciiFrame? CachedFrame { get; set; }

    /// <summary>Source frame sequence when <see cref="CachedFrame"/> was built.</summary>
    public long CachedSequence { get; set; }

    /// <summary>Target convert width used for cache.</summary>
    public int CachedConvertWidth { get; set; }

    /// <summary>Target convert height used for cache.</summary>
    public int CachedConvertHeight { get; set; }

    /// <summary>Palette mode used for cache.</summary>
    public AsciiImagePaletteSource CachedPaletteSource { get; set; }

    /// <summary>Effective charset id used when <see cref="CachedFrame"/> was built (ADR-0080).</summary>
    public string? CachedCharsetId { get; set; }

    /// <summary>
    /// <see cref="Environment.TickCount64"/> when we began waiting for the first frame while the webcam session is active; 0 when not tracking.
    /// </summary>
    public long WaitingForFirstFrameSinceTicks { get; set; }
}
