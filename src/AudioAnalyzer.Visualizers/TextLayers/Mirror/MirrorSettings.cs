using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Mirror. Only MirrorLayer reads these.</summary>
public sealed class MirrorSettings
{
    /// <summary>Which side is the source (the other side is the mirror).</summary>
    [Setting("Direction", "Direction")]
    public MirrorDirection Direction { get; set; } = MirrorDirection.LeftToRight;

    /// <summary>Position of the mirror split as a percentage (25–75). Source and destination regions use the smaller of the two sides for 1:1 mirror.</summary>
    [Setting("Split", "Mirror split %")]
    [SettingRange(25, 75, 25)]
    public int SplitPercent { get; set; } = 50;

    /// <summary>Rotation to apply to the mirrored (destination) region.</summary>
    [Setting("Rotation", "Rotation")]
    public MirrorRotation Rotation { get; set; } = MirrorRotation.None;
}
