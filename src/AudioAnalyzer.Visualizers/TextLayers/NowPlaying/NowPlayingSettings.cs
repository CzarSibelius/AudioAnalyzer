using System.Collections.Generic;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for NowPlaying. Only NowPlayingLayer reads these.</summary>
public sealed class NowPlayingSettings
{
    /// <summary>Fallback phrases when no now-playing session is available (comma-separated in the S modal).</summary>
    [Setting("Snippets", "Snippets")]
    public List<string> TextSnippets { get; set; } = new();

    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public NowPlayingBeatReaction BeatReaction { get; set; } = NowPlayingBeatReaction.None;

    /// <summary>Vertical position of the text in the viewport. Default Center.</summary>
    [SettingChoices("Top", "Center", "Bottom")]
    public string VerticalPosition { get; set; } = "Center";
}
