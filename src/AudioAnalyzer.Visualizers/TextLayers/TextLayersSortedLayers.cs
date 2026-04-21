using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Builds Z-order sorted layer lists shared by <see cref="TextLayersVisualizer"/> and host UI (e.g. layer picker).</summary>
public static class TextLayersSortedLayers
{
    /// <summary>Returns a new list sorted by <see cref="TextLayerSettings.ZOrder"/>, or null when there are no layers.</summary>
    public static List<TextLayerSettings>? BuildSortedByZOrderCopy(TextLayersVisualizerSettings? config)
    {
        if (config?.Layers is not { Count: > 0 })
        {
            return null;
        }

        try
        {
            var snapshot = new List<TextLayerSettings>(config.Layers);
            snapshot.Sort(static (a, b) => a.ZOrder.CompareTo(b.ZOrder));
            return snapshot;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
