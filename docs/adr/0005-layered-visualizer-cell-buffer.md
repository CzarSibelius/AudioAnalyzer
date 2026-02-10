# ADR-0005: Layered visualizer with cell buffer and per-layer config

**Status**: Accepted

## Context

We want a new visualization mode that composites multiple independent layers (e.g. scrolling color background, marquee or falling text) with configurable text snippets and beat-reactive behavior. Existing visualizers write directly to the console line-by-line; to support z-ordered layers (back to front), later-drawn content must overwrite earlier content in the same frame. The renderer currently invokes one visualizer per mode and has no shared framebuffer.

## Decision

1. **Cell buffer for the layered visualizer only**: The new **Layered text** mode is implemented by a single `IVisualizer` that uses a **viewport-sized cell buffer** (width × height). Each cell holds a character and a color (`PaletteColor`). The visualizer clears the buffer, draws each layer in ascending ZOrder into the buffer (so later layers overwrite), then writes the buffer to the console in one pass (line-by-line with ANSI color codes). No other visualizers or the renderer use this buffer; it is internal to the layered text visualizer.

2. **Per-layer config**: Layer list and per-layer settings (type, ZOrder, text snippets, beat reaction, speed, color index) live in **`VisualizerSettings.TextLayers`** (Domain). Config is passed into the visualizer via **`AnalysisSnapshot.TextLayersConfig`**; the renderer sets this only when the current mode is TextLayers. Layers are independent and configurable one at a time via the settings file (e.g. `appsettings.json`).

3. **Layer types and beat reactions**: Supported layer types (e.g. ScrollingColors, Marquee, FallingLetters, MatrixRain, WaveText, StaticText) and beat reactions (None, SpeedBurst, Flash, SpawnMore, Pulse, ColorPop) are defined in Domain enums. The visualizer encapsulates all drawing and state (marquee offset, falling particles, etc.); no new public interfaces are required.

## Consequences

- **Application.Abstractions**: `ViewportCellBuffer` type for compositing; `AnalysisSnapshot.TextLayersConfig` for passing config. Buffer is used only by the TextLayers visualizer.
- **Domain**: `TextLayerType`, `TextLayerBeatReaction`, `TextLayerSettings`, `TextLayersVisualizerSettings`; `VisualizerSettings.TextLayers`; `VisualizationMode.TextLayers`.
- **Infrastructure**: Renderer has a setter for text-layers settings and sets `snapshot.TextLayersConfig` when rendering TextLayers mode. Default layers are created in the settings repository when loading if missing.
- **Visualizers**: One `TextLayersVisualizer` implementation with internal layer strategies and per-layer state; respects viewport bounds and uses the shared palette from the snapshot.
- **Documentation**: README and this ADR document the mode, layer types, beat reactions, and settings structure.
