using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for StaticText. Only StaticTextLayer reads these.</summary>
public sealed class StaticTextSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public StaticTextBeatReaction BeatReaction { get; set; } = StaticTextBeatReaction.None;
}
