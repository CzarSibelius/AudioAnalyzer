# AudioAnalyzer.Visualizers — folder layout

All visual output is **text layers** under **`TextLayers/`**. There is no second top-level visualizer tree (see ADR-0014; ADR-0007 is historical).

**`TextLayers/` root**: shared infrastructure — `TextLayersVisualizer`, `TextLayerDrawContext`, key handler, state store, toolbar helpers, cross-layer utilities (`FileBasedLayerAssetPaths`, `LayerAssetFolder`, etc.).

**`TextLayers/<LayerName>/`**: one folder per layer (e.g. `Fill/`, `Oscilloscope/`, `AsciiModel/`, `FractalZoom/`). Co-locate `*Layer.cs`, `*Settings.cs`, layer-specific helpers, and generated assets used only by that layer.

## Rules

- New visual content: add a layer folder under `TextLayers/`, register the renderer in `TextLayersVisualizer`, follow [docs/agents/visualizers.md](../visualizers.md).
- Do not add new standalone `IVisualizer` implementations or parallel roots like `src/AudioAnalyzer.Visualizers/Geiss/`.
- Namespace remains `AudioAnalyzer.Visualizers` (and nested where already used); folders are organizational.
