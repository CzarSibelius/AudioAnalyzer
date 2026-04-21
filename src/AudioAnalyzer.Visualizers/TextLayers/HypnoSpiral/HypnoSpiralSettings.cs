using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for <see cref="TextLayerType.HypnoSpiral"/>.</summary>
public sealed class HypnoSpiralSettings
{
    /// <summary>Number of spiral arms in the interference pattern.</summary>
    [Setting("ArmCount", "Arm count")]
    [SettingRange(2, 24, 1)]
    public int ArmCount { get; set; } = 10;

    /// <summary>Log-radius multiplier inside the sine (higher = tighter winding).</summary>
    [Setting("LogPitch", "Log pitch")]
    [SettingRange(2, 22, 0.5)]
    public double LogPitch { get; set; } = 9.0;

    /// <summary>How many full pattern rotations advance per musical beat at the detected BPM.</summary>
    [Setting("RevolutionsPerBeat", "Revolutions per beat")]
    [SettingRange(0.125, 6, 0.125)]
    public double RevolutionsPerBeat { get; set; } = 1.0;

    /// <summary>Blend of the secondary shifted pattern (0 = single wave, 1 = equal mix).</summary>
    [Setting("MoireMix", "Moire mix")]
    [SettingRange(0, 1, 0.05)]
    public double MoireMix { get; set; } = 0.55;

    /// <summary>Phase offset between the two waves (radians).</summary>
    [Setting("MoirePhase", "Moire phase")]
    [SettingRange(0, 6.283, 0.1)]
    public double MoirePhase { get; set; } = 1.2;

    /// <summary>Frequency multiplier for the second wave (slightly off 1.0 strengthens moiré).</summary>
    [Setting("MoireDetune", "Moire detune")]
    [SettingRange(0.92, 1.08, 0.01)]
    public double MoireDetune { get; set; } = 1.03;

    /// <summary>Radians per second added to <see cref="MoirePhase"/> drift.</summary>
    [Setting("MoireDriftSpeed", "Moire drift speed")]
    [SettingRange(0, 1.5, 0.05)]
    public double MoireDriftSpeed { get; set; } = 0.15;

    /// <summary>Optional beat flash / speed boost.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public HypnoSpiralBeatReaction BeatReaction { get; set; } = HypnoSpiralBeatReaction.None;

    /// <summary>Charset id for density glyphs (<c>charsets/*.json</c>, ADR-0080). Unset uses <see cref="CharsetIds.DensitySoft"/>.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }
}
