namespace AudioAnalyzer.Visualizers;

/// <summary>Builds the optional toolbar suffix string for the TextLayers visualizer (layer digits, hints, gain, palette).</summary>
public interface ITextLayersToolbarBuilder
{
    /// <summary>Returns the toolbar suffix, or null if no layers. Empty config returns a short hint.</summary>
    string? BuildSuffix(TextLayersToolbarContext context);
}
