namespace AudioAnalyzer.Domain;

/// <summary>
/// Character set as stored in a JSON file under <c>charsets/</c> (ADR-0080). The string is an ordered sequence of Unicode code points (including spaces).
/// </summary>
public sealed class CharsetDefinition
{
    /// <summary>Optional display name for lists.</summary>
    public string? Name { get; set; }

    /// <summary>Ordered characters used when mapping brightness, density, or random glyphs.</summary>
    public string Characters { get; set; } = "";
}
