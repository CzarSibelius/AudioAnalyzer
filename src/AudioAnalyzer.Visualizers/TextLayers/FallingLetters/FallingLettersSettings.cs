using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for FallingLetters. Only FallingLettersLayer reads these.</summary>
public sealed class FallingLettersSettings
{
    /// <summary>Particle rain vs column-style rain (merged former MatrixRain).</summary>
    [Setting("AnimationMode", "Animation")]
    public FallingLettersAnimationMode AnimationMode { get; set; } = FallingLettersAnimationMode.Particles;

    /// <summary>How a beat affects this layer. Default None. <see cref="FallingLettersBeatReaction.SpawnMore"/> and <see cref="FallingLettersBeatReaction.SpeedBurst"/> apply to <see cref="FallingLettersAnimationMode.Particles"/> only.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public FallingLettersBeatReaction BeatReaction { get; set; } = FallingLettersBeatReaction.None;

    /// <summary>Glyph pool from <c>charsets/*.json</c> (ADR-0080). When unset, <see cref="CharsetIds.Digits"/> is used.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }
}
