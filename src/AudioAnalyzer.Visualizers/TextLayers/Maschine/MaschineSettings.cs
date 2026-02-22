using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Maschine. Only MaschineLayer reads these.</summary>
public sealed class MaschineSettings
{
    /// <summary>Palette index for the aligned diagonal (accent) color.</summary>
    [Setting("AccentColorIndex", "Accent color index")]
    [SettingRange(0, 31, 1)]
    public int AccentColorIndex { get; set; } = 1;

    /// <summary>Whether the accent column is fixed (leftmost) or moves with beat phase.</summary>
    [Setting("AccentColumnMode", "Accent column")]
    public MaschineAccentColumnMode AccentColumnMode { get; set; } = MaschineAccentColumnMode.Moving;
}
