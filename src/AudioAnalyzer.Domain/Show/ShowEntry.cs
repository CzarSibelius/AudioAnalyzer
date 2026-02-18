namespace AudioAnalyzer.Domain;

/// <summary>
/// A single entry in a Show: a preset reference and how long it plays.
/// </summary>
public class ShowEntry
{
    /// <summary>Id of the preset to display. Resolved via IPresetRepository.</summary>
    public string PresetId { get; set; } = "";

    /// <summary>How long this preset plays before advancing to the next.</summary>
    public DurationConfig Duration { get; set; } = new();
}
