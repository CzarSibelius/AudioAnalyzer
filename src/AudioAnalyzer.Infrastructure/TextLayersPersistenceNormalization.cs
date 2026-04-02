using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>Caps <see cref="TextLayersVisualizerSettings.Layers"/> to <see cref="TextLayersLimits.MaxLayerCount"/> when loading from disk. Per ADR-0070, does not pad to the maximum.</summary>
internal static class TextLayersPersistenceNormalization
{
    /// <summary>Ensures <c>Layers</c> is non-null and trims to <see cref="TextLayersLimits.MaxLayerCount"/> if oversized.</summary>
    public static void NormalizeLayerList(TextLayersVisualizerSettings textLayers)
    {
        textLayers.Layers ??= new List<TextLayerSettings>();
        if (textLayers.Layers.Count > TextLayersLimits.MaxLayerCount)
        {
            textLayers.Layers = textLayers.Layers.OrderBy(l => l.ZOrder).Take(TextLayersLimits.MaxLayerCount).ToList();
        }
    }
}
