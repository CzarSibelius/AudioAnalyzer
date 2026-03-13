using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for GeissBackground. Only GeissBackgroundLayer reads these.</summary>
public sealed class GeissBackgroundSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public GeissBackgroundBeatReaction BeatReaction { get; set; } = GeissBackgroundBeatReaction.None;
}
