namespace AudioAnalyzer.Domain;

/// <summary>Settings for the Geiss visualizer.</summary>
public class GeissVisualizerSettings
{
    /// <summary>Show expanding circles on beat.</summary>
    public bool BeatCircles { get; set; } = true;

    /// <summary>Id of the selected color palette (e.g. filename without extension). Resolved via IPaletteRepository.</summary>
    public string? PaletteId { get; set; }
}
