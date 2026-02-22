# Visualizer specs

This directory contains per-layer and TextLayers reference documentation: behavior, settings, viewport constraints, and implementation notes. ADRs in `docs/adr/` describe architectural decisions; these specs describe *what each layer does* and how to work with it.

## Architecture

The app has **one visualizer**: `TextLayersVisualizer` (implements `IVisualizer`). All visual content — oscilloscope, VU meters, spectrum bars, plasma backgrounds, beat circles, etc. — comes from configurable `ITextLayerRenderer` layers. Users compose views by adding layers to presets; **V** cycles presets, **S** edits layer settings. See [ADR-0014](../adr/0014-visualizers-as-layers.md).

## Index

The first row is the visualizer; the rest are **layer types** within it. Press **←/→** to set a layer's type.

| Kind | TechnicalName | Display Name | Spec | Description |
|------|---------------|--------------|------|--------------|
| Visualizer | textlayers | Layered text | [text-layers.md](text-layers.md) | Presets (V to cycle); multiple configurable layers (GeissBackground, BeatCircles, Oscilloscope, VuMeter, LlamaStyle, UnknownPleasures, etc.) with beat reactions |
| Layer | oscilloscope | Oscilloscope | [oscilloscope.md](oscilloscope.md) | Time-domain waveform; gain adjustable with [ ] when selected |
| Layer | vumeter | VU Meter | [vu-meter-layer.md](vu-meter-layer.md) | Stereo channel levels and balance |
| Layer | llamastyle | Llama Style | [llama-style.md](llama-style.md) | Classic spectrum bars with configurable options (volume bar, labels, color scheme) |
| Layer (deprecated) | geiss | Geiss | [geiss.md](geiss.md) | Removed; use GeissBackground and BeatCircles layers |
| Layer | unknownpleasures | Unknown Pleasures | [unknown-pleasures.md](unknown-pleasures.md) | Stacked waveform snapshots, beat-triggered |
| Layer | asciiimage | ASCII Image | [ascii-image.md](ascii-image.md) | Images as ASCII art from folder; scroll/zoom; layer palette or image colors |
| Layer | mirror | Mirror | [mirror.md](mirror.md) | Mirrors buffer horizontally or vertically (direction, split %, rotation); place above layers to mirror |
| Layer | fill | Fill | [fill.md](fill.md) | Full-viewport fill with configured color and fill character (block, shades, space, or custom ASCII) |

## Implementation layout

All visual content lives in `src/AudioAnalyzer.Visualizers/TextLayers/<LayerName>/` — one subfolder per layer type (e.g. `Oscilloscope/`, `VuMeter/`, `LlamaStyle/`). The only `IVisualizer` is `TextLayersVisualizer`; there are no standalone visualizer subfolders. Per-layer settings types (e.g. `TextLayerSettings`, `LlamaStyleSettings`) live in the Domain project or next to the layer. See [ADR-0007](../adr/0007-visualizer-subfolder-structure.md) and [ADR-0010](../adr/0010-appsettings-visualizer-settings-separation.md) for the rationale.

## For agents

When adding or changing visualizer content:

1. **Add layers, not standalone visualizers** — new content must be an `ITextLayerRenderer` layer, not a new `IVisualizer`. See [ADR-0014](../adr/0014-visualizers-as-layers.md).
2. **Read the relevant spec** in `docs/visualizers/` and the index above.
3. **Follow the viewport rule**: `.cursor/rules/visualizers-viewport.mdc` — respect viewport bounds.
4. **Follow ADRs**: [ADR-0004](../adr/0004-visualizer-encapsulation.md) (encapsulation), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md) (TextLayers cell buffer), [ADR-0008](../adr/0008-visualizer-settings-di.md) (settings via constructor injection).
5. **Create or update the spec** when adding or changing a layer; keep it in sync with the implementation.

## Spec format

Each spec file follows: Description, Snapshot usage, Settings, Key bindings, Viewport constraints, Implementation notes.
