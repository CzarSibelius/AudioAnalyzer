using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for ScrollingColors. Only ScrollingColorsLayer reads these.</summary>
public sealed class ScrollingColorsSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public ScrollingColorsBeatReaction BeatReaction { get; set; } = ScrollingColorsBeatReaction.None;
}
