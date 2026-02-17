using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for NowPlaying. Only NowPlayingLayer reads these.</summary>
public sealed class NowPlayingSettings
{
    /// <summary>Vertical position of the text in the viewport. Default Center.</summary>
    [SettingChoices("Top", "Center", "Bottom")]
    public string VerticalPosition { get; set; } = "Center";
}
