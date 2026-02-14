namespace AudioAnalyzer.Domain;

/// <summary>
/// Container for per-visualizer settings. Each visualizer that needs configuration has its own property here.
/// </summary>
public class VisualizerSettings
{
    public UnknownPleasuresVisualizerSettings? UnknownPleasures { get; set; }
    public GeissVisualizerSettings? Geiss { get; set; }
    public OscilloscopeVisualizerSettings? Oscilloscope { get; set; }
    public TextLayersVisualizerSettings? TextLayers { get; set; }
}
