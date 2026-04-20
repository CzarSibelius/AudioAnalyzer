using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="StarfieldLayer"/> (ADR-0082).</summary>
public sealed class StarfieldSettings
{
    /// <summary>Number of stars (clamped to <see cref="StarfieldLayer.MaxStarHardCap"/> at runtime).</summary>
    [Setting("StarCount", "Star count")]
    [SettingRange(20, 600, 10)]
    public int StarCount { get; set; } = 220;

    /// <summary>Travel speed scale (combined with layer <c>SpeedMultiplier</c> and frame delta).</summary>
    [Setting("BaseSpeed", "Base speed")]
    [SettingRange(0.1, 4.0, 0.1)]
    public double BaseSpeed { get; set; } = 1.0;

    /// <summary>Approximate wall seconds to move through the full Z range at <see cref="BaseSpeed"/> 1.0 and default speed factors (larger = calmer, more “endless cruise”).</summary>
    [Setting("TravelSeconds", "Travel seconds")]
    [SettingRange(12.0, 120.0, 2)]
    public double TravelSeconds { get; set; } = 32.0;

    /// <summary>Horizontal drift of the vanishing point in cells per second.</summary>
    [Setting("CenterDriftX", "Center drift X")]
    [SettingRange(-12.0, 12.0, 0.5)]
    public double CenterDriftX { get; set; }

    /// <summary>Vertical drift of the vanishing point in cells per second.</summary>
    [Setting("CenterDriftY", "Center drift Y")]
    [SettingRange(-12.0, 12.0, 0.5)]
    public double CenterDriftY { get; set; }

    /// <summary>Static view-center offset in cells (added each frame, not accumulated).</summary>
    [Setting("ViewCenterOffsetX", "View offset X")]
    [SettingRange(-40.0, 40.0, 1)]
    public double ViewCenterOffsetX { get; set; }

    /// <summary>Static view-center offset in cells.</summary>
    [Setting("ViewCenterOffsetY", "View offset Y")]
    [SettingRange(-40.0, 40.0, 1)]
    public double ViewCenterOffsetY { get; set; }

    /// <summary>Incremental XY rotation in radians per second (field “turns” around Z).</summary>
    [Setting("TumbleRadiansPerSecond", "Tumble rad/s")]
    [SettingRange(-0.4, 0.4, 0.02)]
    public double TumbleRadiansPerSecond { get; set; }

    /// <summary>Half-width of spawn volume in model units.</summary>
    [Setting("SpreadX", "Spread X")]
    [SettingRange(0.5, 8.0, 0.1)]
    public double SpreadX { get; set; } = 2.2;

    /// <summary>Half-height of spawn volume in model units.</summary>
    [Setting("SpreadY", "Spread Y")]
    [SettingRange(0.5, 8.0, 0.1)]
    public double SpreadY { get; set; } = 2.2;

    /// <summary>Perspective scale (larger = wider spread on screen).</summary>
    [Setting("FocalLength", "Focal length")]
    [SettingRange(4.0, 120.0, 1)]
    public double FocalLength { get; set; } = 28.0;

    /// <summary>Near clipping depth (stars respawn when Z is at or below this value).</summary>
    [Setting("ZNear", "Z near")]
    [SettingRange(0.08, 6.0, 0.02)]
    public double ZNear { get; set; } = 0.35;

    /// <summary>Far depth where stars (re)spawn.</summary>
    [Setting("ZFar", "Z far")]
    [SettingRange(1.0, 200.0, 1)]
    public double ZFar { get; set; } = 90.0;

    /// <summary>Vertical cell aspect for projection (1 = square cells; default matches ADR-0082).</summary>
    [Setting("CellAspect", "Cell aspect")]
    [SettingRange(1.0, 3.0, 0.05)]
    public double CellAspect { get; set; } = StarfieldProjection.DefaultCellAspect;

    /// <summary>How palette indices are chosen.</summary>
    [Setting("DepthShading", "Depth shading")]
    public StarfieldDepthShading DepthShading { get; set; } = StarfieldDepthShading.DepthGradient;

    /// <summary>Beat-driven behavior.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public StarfieldBeatReaction BeatReaction { get; set; } = StarfieldBeatReaction.None;

    /// <summary>When &gt;= 0, spawns use <see cref="Random"/> with this seed; when &lt; 0, use <see cref="Random.Shared"/>.</summary>
    [Setting("FixedRandomSeed", "Fixed seed")]
    [SettingRange(-1, 999_999, 1)]
    public int FixedRandomSeed { get; set; } = -1;

    /// <summary>Charset id for star glyphs (<c>charsets/*.json</c>, ADR-0080). Unset uses <see cref="CharsetIds.DensitySoft"/>.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }
}
