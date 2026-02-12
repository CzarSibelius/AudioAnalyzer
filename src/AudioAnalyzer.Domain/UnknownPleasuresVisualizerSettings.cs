namespace AudioAnalyzer.Domain;

/// <summary>Settings for the Unknown Pleasures visualizer.</summary>
public class UnknownPleasuresVisualizerSettings
{
    /// <summary>Color palette for the stacked waveform lines. Null/empty uses the default palette. Legacy; prefer PaletteId.</summary>
    public ColorPalette? Palette { get; set; }

    /// <summary>Id of the selected color palette (e.g. filename without extension). Resolved via IPaletteRepository.</summary>
    public string? PaletteId { get; set; }
}
