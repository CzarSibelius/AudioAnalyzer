using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for FallingLetters. Only FallingLettersLayer reads these.</summary>
public sealed class FallingLettersSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public FallingLettersBeatReaction BeatReaction { get; set; } = FallingLettersBeatReaction.None;
}
