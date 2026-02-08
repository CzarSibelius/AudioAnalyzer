namespace AudioAnalyzer.Domain;

/// <summary>Settings for the Unknown Pleasures visualizer.</summary>
public class UnknownPleasuresVisualizerSettings
{
    /// <summary>Color palette for the stacked waveform lines. Null/empty uses the default palette.</summary>
    public ColorPalette? Palette { get; set; }
}
