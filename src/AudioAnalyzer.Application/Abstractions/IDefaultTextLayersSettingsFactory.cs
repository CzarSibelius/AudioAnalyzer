using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Supplies default TextLayers configuration using layer-specific custom settings types.
/// Implemented in Visualizers so persistence (Infrastructure) does not reference layer POCOs.
/// </summary>
public interface IDefaultTextLayersSettingsFactory
{
    /// <summary>
    /// Default preset: <see cref="TextLayersLimits.MaxLayerCount"/> layers with varied types. Keys 1–9 map to layers 1–9.
    /// </summary>
    TextLayersVisualizerSettings CreateDefault();

    /// <summary>
    /// Marquee layer used when padding layer count up to <see cref="TextLayersLimits.MaxLayerCount"/>.
    /// </summary>
    /// <param name="zOrder">Draw order for the new layer.</param>
    /// <param name="displayLayerNumber">1-based label for <see cref="TextLayerSettings.TextSnippets"/> (e.g. "Layer 3").</param>
    TextLayerSettings CreatePaddingMarqueeLayer(int zOrder, int displayLayerNumber);
}
