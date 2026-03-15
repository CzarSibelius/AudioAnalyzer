namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Builds the optional toolbar suffix string or labeled viewports for the TextLayers visualizer (layer digits, gain, palette).</summary>
public interface ITextLayersToolbarBuilder
{
    /// <summary>Returns the toolbar suffix, or null if no layers. Empty config returns a short hint.</summary>
    string? BuildSuffix(TextLayersToolbarContext context);

    /// <summary>Returns toolbar descriptors with own labels (Layers, optional Gain, Palette). Empty when no layers.</summary>
    IReadOnlyList<LabeledValueDescriptor> BuildViewports(TextLayersToolbarContext context);
}
