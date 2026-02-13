# Visualizer specs

This directory contains per-visualizer reference documentation: behavior, settings, viewport constraints, and implementation notes. ADRs in `docs/adr/` describe architectural decisions; these specs describe *what each visualizer does* and how to work with it.

## Index

| TechnicalName | Display Name | Spec | Description |
|--------------|--------------|------|--------------|
| spectrum | Spectrum Analyzer | [spectrum-bars.md](spectrum-bars.md) | Volume bar + frequency bars with peak hold and labels |
| oscilloscope | Oscilloscope | [oscilloscope.md](oscilloscope.md) | Time-domain waveform with adjustable gain |
| vumeter | VU Meter | [vu-meter.md](vu-meter.md) | Stereo channel levels and balance |
| winamp | Winamp Style | [winamp-bars.md](winamp-bars.md) | Classic spectrum bars |
| geiss | Geiss | [geiss.md](geiss.md) | Psychedelic plasma with optional beat circles |
| unknownpleasures | Unknown Pleasures | [unknown-pleasures.md](unknown-pleasures.md) | Stacked waveform snapshots, beat-triggered |
| textlayers | Layered text | [text-layers.md](text-layers.md) | Multiple configurable layers with beat reactions |

## Implementation layout

Visualizer code lives in `src/AudioAnalyzer.Visualizers/<VisualizerName>/`. Each visualizer has its own subfolder (e.g. `Geiss`, `Oscilloscope`, `SpectrumBars`). Per-visualizer settings types (e.g. `TextLayerSettings`, `GeissVisualizerSettings`) also live in their visualizer subfolder. See [ADR-0007](../adr/0007-visualizer-subfolder-structure.md) and [ADR-0010](../adr/0010-appsettings-visualizer-settings-separation.md) for the rationale.

## For agents

When adding or changing a visualizer:

1. **Read the relevant spec** in `docs/visualizers/` and the index above.
2. **Follow the viewport rule**: `.cursor/rules/visualizers-viewport.mdc` — respect viewport bounds.
3. **Follow ADRs**: [ADR-0004](../adr/0004-visualizer-encapsulation.md) (encapsulation), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md) (TextLayers cell buffer), [ADR-0008](../adr/0008-visualizer-settings-di.md) (settings via constructor injection), [ADR-0014](../adr/0014-visualizers-as-layers.md) (new visualizers as layers, not standalone IVisualizer).
4. **Create or update the spec** when adding or changing a visualizer; keep it in sync with the implementation.

## Spec format

Each spec file follows: Description, Snapshot usage, Settings, Key bindings, Viewport constraints, Implementation notes.
