# ADR-0014: Visualizers as layers — deprecate IVisualizer for new development

**Status**: Accepted

## Context

The current `IVisualizer` model creates standalone visualization modes: each visualizer (SpectrumBars, Geiss, Oscilloscope, etc.) is one mode, and the user switches between them with the V key. They cannot be combined. In contrast, the **Layered text** mode (`TextLayersVisualizer`) uses text layer renderers (classes inheriting `TextLayerRendererBase`) that draw into a shared cell buffer; users configure multiple layers (e.g. ScrollingColors, GeissBackground, BeatCircles) with ZOrder and can mix and match any combination. The goal is to have all visualizer content available as layers so users can compose their own views (e.g. spectrum bars + Geiss background + beat circles in one display).

## Decision

1. **Legacy approach (deprecated for new development)**: Implementing `IVisualizer` directly to create a standalone visualization mode is the old way. SpectrumBars, VuMeter, and WinampBars have been migrated to layers (VuMeter, LlamaStyle). The app now uses only TextLayers mode; all visualizer content is implemented as text layer renderers (TextLayerRendererBase). No new standalone `IVisualizer` modes should be added.

2. **Preferred approach**: New visualizers must be created as text layer renderers, hosted by `TextLayersVisualizer`. To add a new layer:
   - Inherit `TextLayerRendererBase` and implement `ITextLayerRenderer<TState>` with a distinct `LayerType`
   - Add the new type to the `TextLayerType` enum (Domain)
   - Register the renderer in `TextLayersVisualizer.CreateRenderers()`
   - Add per-layer settings to `TextLayerSettings` if needed
   - Document the layer in `docs/visualizers/text-layers.md`

   Layers plug into the Layered text mode where users can combine them with other layers.

3. **Migration**: SpectrumBars, VuMeter, and WinampBars were migrated to layers (VuMeter, LlamaStyle). Users with legacy `spectrum`, `vumeter`, or `winamp` in settings are auto-migrated to TextLayers with the corresponding layer added.

## Consequences

- **New development**: Agents and developers must create new visualizer content as text layer renderers (TextLayerRendererBase + ITextLayerRenderer&lt;TState&gt;), not as new `IVisualizer` implementations.
- **Existing modes**: The app uses only TextLayers; all visualizer content (VuMeter, LlamaStyle, Oscilloscope, UnknownPleasures, GeissBackground, BeatCircles, etc.) is provided as layers.
- **Documentation**: README, agent instructions, and visualizer specs reference ADR-0014 and steer toward the layer approach.
- **References**: [TextLayerRendererBase](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayerRendererBase.cs), [TextLayersVisualizer](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs), [ADR-0005](0005-layered-visualizer-cell-buffer.md).
