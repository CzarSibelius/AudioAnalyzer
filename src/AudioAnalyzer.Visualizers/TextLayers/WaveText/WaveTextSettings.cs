using System.Collections.Generic;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for WaveText. Only WaveTextLayer reads these.</summary>
public sealed class WaveTextSettings
{
    /// <summary>One or more phrases shown by the layer (comma-separated in the S modal).</summary>
    [Setting("Snippets", "Snippets")]
    public List<string> TextSnippets { get; set; } = new();

    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public WaveTextBeatReaction BeatReaction { get; set; } = WaveTextBeatReaction.None;
}
