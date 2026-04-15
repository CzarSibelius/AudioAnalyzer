using System.Collections.Generic;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Marquee. Only MarqueeLayer reads these.</summary>
public sealed class MarqueeSettings
{
    /// <summary>One or more phrases shown by the layer (comma-separated in the S modal).</summary>
    [Setting("Snippets", "Snippets")]
    public List<string> TextSnippets { get; set; } = new();

    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public MarqueeBeatReaction BeatReaction { get; set; } = MarqueeBeatReaction.None;
}
