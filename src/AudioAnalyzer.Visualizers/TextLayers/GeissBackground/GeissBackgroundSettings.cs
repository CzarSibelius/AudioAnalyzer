using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for GeissBackground. Only GeissBackgroundLayer reads these.</summary>
public sealed class GeissBackgroundSettings
{
    /// <summary>How a beat affects this layer. Default None.</summary>
    [Setting("BeatReaction", "Beat reaction")]
    public GeissBackgroundBeatReaction BeatReaction { get; set; } = GeissBackgroundBeatReaction.None;

    /// <summary>Charset id for plasma density glyphs (<c>charsets/*.json</c>, ADR-0080). Unset uses <see cref="CharsetIds.DensitySoft"/>.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }
}
