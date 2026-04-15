using System.Collections.Generic;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for StaticText. Only StaticTextLayer reads these.</summary>
public sealed class StaticTextSettings
{
    /// <summary>One or more phrases shown by the layer (comma-separated in the S modal).</summary>
    [Setting("Snippets", "Snippets")]
    public List<string> TextSnippets { get; set; } = new();

    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public StaticTextBeatReaction BeatReaction { get; set; } = StaticTextBeatReaction.None;
}
