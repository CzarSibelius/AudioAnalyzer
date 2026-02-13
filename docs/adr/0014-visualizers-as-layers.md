# ADR-0014: Visualizers as layers — deprecate IVisualizer for new development

**Status**: Accepted

## Context

The current `IVisualizer` model creates standalone visualization modes: each visualizer (SpectrumBars, Geiss, Oscilloscope, etc.) is one mode, and the user switches between them with the V key. They cannot be combined. In contrast, the **Layered text** mode (`TextLayersVisualizer`) uses `ITextLayerRenderer` implementations that draw into a shared cell buffer; users configure multiple layers (e.g. ScrollingColors, GeissBackground, BeatCircles) with ZOrder and can mix and match any combination. The goal is to have all visualizer content available as layers so users can compose their own views (e.g. spectrum bars + Geiss background + beat circles in one display).

## Decision

1. **Legacy approach (deprecated for new development)**: Implementing `IVisualizer` directly to create a standalone visualization mode is the old way. Existing implementations (SpectrumBars, VuMeter, WinampBars, Oscilloscope, Geiss, UnknownPleasures) remain for backward compatibility. No new standalone `IVisualizer` modes should be added.

2. **Preferred approach**: New visualizers must be created as `ITextLayerRenderer` implementations, hosted by `TextLayersVisualizer`. To add a new layer:
   - Implement `ITextLayerRenderer` with a distinct `LayerType`
   - Add the new type to the `TextLayerType` enum (Domain)
   - Register the renderer in `TextLayersVisualizer.CreateRenderers()`
   - Add per-layer settings to `TextLayerSettings` if needed
   - Document the layer in `docs/visualizers/text-layers.md`

   Layers plug into the Layered text mode where users can combine them with other layers.

3. **Migration**: Existing standalone modes may be migrated to layers over time. The long-term direction is layers-only; migration is optional and incremental.

## Consequences

- **New development**: Agents and developers must create new visualizer content as `ITextLayerRenderer` layers, not as new `IVisualizer` implementations.
- **Existing modes**: Standalone `IVisualizer` modes stay; no removal required. They remain separate modes until (and if) migrated.
- **Documentation**: README, agent instructions, and visualizer specs reference ADR-0014 and steer toward the layer approach.
- **References**: [ITextLayerRenderer](../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerRenderer.cs), [TextLayersVisualizer.CreateRenderers()](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs), [ADR-0005](0005-layered-visualizer-cell-buffer.md).
