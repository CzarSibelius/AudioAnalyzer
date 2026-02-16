# Visualizer specs

This directory contains per-visualizer reference documentation: behavior, settings, viewport constraints, and implementation notes. ADRs in `docs/adr/` describe architectural decisions; these specs describe *what each visualizer does* and how to work with it.

## Index

| TechnicalName | Display Name | Spec | Description |
|--------------|--------------|------|--------------|
| textlayers | Layered text | [text-layers.md](text-layers.md) | Presets (V to cycle); multiple configurable layers (GeissBackground, BeatCircles, Oscilloscope, VuMeter, LlamaStyle, UnknownPleasures, etc.) with beat reactions |
| oscilloscope | Oscilloscope (layer) | [oscilloscope.md](oscilloscope.md) | Time-domain waveform layer in Layered text; gain adjustable with [ ] when selected |
| vumeter | VU Meter (layer) | [vu-meter-layer.md](vu-meter-layer.md) | Stereo channel levels and balance |
| llamastyle | Llama Style (layer) | [llama-style.md](llama-style.md) | Classic spectrum bars with configurable options (volume bar, labels, color scheme) |
| geiss | Geiss (deprecated) | [geiss.md](geiss.md) | Removed; use GeissBackground and BeatCircles layers in Layered text |
| unknownpleasures | Unknown Pleasures (layer) | [unknown-pleasures.md](unknown-pleasures.md) | Stacked waveform snapshots, beat-triggered; use in Layered text via layer type |

## Implementation layout

Visualizer code lives in `src/AudioAnalyzer.Visualizers/<VisualizerName>/` or `src/AudioAnalyzer.Visualizers/TextLayers/<LayerName>/`. The app uses Layered text mode only; all visualizer content (VuMeter, LlamaStyle, Oscilloscope, UnknownPleasures, GeissBackground, BeatCircles, etc.) is implemented as `ITextLayerRenderer` layers. Per-visualizer settings types (e.g. `TextLayerSettings`) live in the Domain project. See [ADR-0007](../adr/0007-visualizer-subfolder-structure.md) and [ADR-0010](../adr/0010-appsettings-visualizer-settings-separation.md) for the rationale.

## For agents

When adding or changing a visualizer:

1. **Read the relevant spec** in `docs/visualizers/` and the index above.
2. **Follow the viewport rule**: `.cursor/rules/visualizers-viewport.mdc` — respect viewport bounds.
3. **Follow ADRs**: [ADR-0004](../adr/0004-visualizer-encapsulation.md) (encapsulation), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md) (TextLayers cell buffer), [ADR-0008](../adr/0008-visualizer-settings-di.md) (settings via constructor injection), [ADR-0014](../adr/0014-visualizers-as-layers.md) (new visualizers as layers, not standalone IVisualizer).
4. **Create or update the spec** when adding or changing a visualizer; keep it in sync with the implementation.

## Spec format

Each spec file follows: Description, Snapshot usage, Settings, Key bindings, Viewport constraints, Implementation notes.
