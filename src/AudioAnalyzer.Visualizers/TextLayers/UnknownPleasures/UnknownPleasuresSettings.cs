using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Unknown Pleasures. Only <see cref="UnknownPleasuresLayer"/> reads these.</summary>
public sealed class UnknownPleasuresSettings
{
    /// <summary>Charset id for magnitude→glyph mapping (<c>charsets/*.json</c>, ADR-0080). Unset uses <see cref="CharsetIds.UnknownPleasuresRamp"/>.</summary>
    [Setting("CharsetId", "Charset")]
    [CharsetSetting]
    public string? CharsetId { get; set; }
}
