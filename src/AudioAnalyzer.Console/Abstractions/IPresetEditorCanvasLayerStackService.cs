using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Add/remove text layers from the Preset editor main canvas with the same rules as the S modal layer list (ADR-0070).
/// </summary>
internal interface IPresetEditorCanvasLayerStackService
{
    /// <summary>Adds a layer when below max count; selects the new sorted slot. Returns false if at capacity.</summary>
    bool TryInsertLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer);

    /// <summary>Removes the active sorted-slot layer when any exist. Returns false if there is nothing to remove.</summary>
    bool TryDeleteActiveLayer(VisualizerSettings visualizerSettings, IVisualizer visualizer);
}
