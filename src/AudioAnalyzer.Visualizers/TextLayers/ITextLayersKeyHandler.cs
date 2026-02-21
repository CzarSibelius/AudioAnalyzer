using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Visualizers;

/// <summary>Handles mode-specific keys for the TextLayers visualizer: 1–9 select/toggle, P palette, [ ] gain, I next image, Left/Right cycle type.</summary>
public interface ITextLayersKeyHandler
{
    /// <summary>Handles a key. Mutates context (e.g. PaletteCycleLayerIndex, layer settings). Returns true if the key was consumed.</summary>
    bool Handle(ConsoleKeyInfo key, TextLayersKeyContext context);
}
