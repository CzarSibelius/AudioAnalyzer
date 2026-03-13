using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for WaveText. Only WaveTextLayer reads these.</summary>
public sealed class WaveTextSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public WaveTextBeatReaction BeatReaction { get; set; } = WaveTextBeatReaction.None;
}
