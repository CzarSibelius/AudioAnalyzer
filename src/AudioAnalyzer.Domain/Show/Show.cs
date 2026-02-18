namespace AudioAnalyzer.Domain;

/// <summary>
/// An ordered collection of presets with per-preset duration. Used for live performance auto-cycling.
/// </summary>
public class Show
{
    /// <summary>Stable identifier (e.g. filename without extension).</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Display name. User-editable.</summary>
    public string Name { get; set; } = "Show 1";

    /// <summary>Ordered list of preset entries. Playback iterates through this list.</summary>
    public List<ShowEntry> Entries { get; set; } = new();
}
