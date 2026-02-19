using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Mirror. Only MirrorLayer reads these.</summary>
public sealed class MirrorSettings
{
    /// <summary>Which side is the source (the other side is the mirror).</summary>
    [Setting("Direction", "Direction")]
    public MirrorDirection Direction { get; set; } = MirrorDirection.LeftToRight;
}
