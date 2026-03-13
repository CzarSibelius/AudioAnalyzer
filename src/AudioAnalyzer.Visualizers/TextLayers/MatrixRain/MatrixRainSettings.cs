using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for MatrixRain. Only MatrixRainLayer reads these.</summary>
public sealed class MatrixRainSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public MatrixRainBeatReaction BeatReaction { get; set; } = MatrixRainBeatReaction.None;
}
