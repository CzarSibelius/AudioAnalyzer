using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Marquee. Only MarqueeLayer reads these.</summary>
public sealed class MarqueeSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public MarqueeBeatReaction BeatReaction { get; set; } = MarqueeBeatReaction.None;
}
