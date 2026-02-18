# Unknown Pleasures (layer)

## Description

Stacked waveform snapshots inspired by the pulsar plot. This layer type is part of TextLayersVisualizer; there is no standalone Unknown Pleasures visualizer. The bottom line is always realtime; the others are beat-triggered frozen snapshots. Gaps between each pulse. Uses the layer's palette (per-layer `PaletteId`).

## Snapshot usage

- `SmoothedMagnitudes` — spectrum magnitudes for pulse lines
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization
- `BeatCount` — triggers a new frozen snapshot when it changes
- Palette — resolved per layer from `PaletteId` (or `TextLayers.PaletteId`)

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `UnknownPleasures`
- **PaletteId** (string, optional): Id of the color palette for this layer. Inherits from `TextLayers.PaletteId` when null/empty. Press **P** when this layer is selected to cycle and save.

## Key bindings

- **1–9** — Select layer (when Unknown Pleasures layer is selected, **P** cycles its palette)
- **←/→** — Cycle layer type to Unknown Pleasures

## Viewport constraints

- Minimum width: 20
- Minimum height: 5 lines
- Uses full `ctx.Width` × `ctx.Height` for the layer
- Each snapshot block: 3 pulse lines + 1 gap line (4 rows per snapshot)
- Bottom block uses live data; others use frozen snapshots

## Implementation notes

- **Implementation**: `TextLayers/UnknownPleasures/UnknownPleasuresLayer.cs`
- **State**: `UnknownPleasuresState` — snapshots (up to 14), live pulse, last beat count, color offset
- **Snapshot width**: Fixed 120 samples; mapped to viewport width when rendering
- **ASCII gradient**: ` .,'-_\"/~*#` — maps normalized magnitude to character density across three line bands
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md).
