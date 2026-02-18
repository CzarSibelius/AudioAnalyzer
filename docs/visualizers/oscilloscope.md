# Oscilloscope (layer only)

## Description

Time-domain waveform display showing audio amplitude over time. This layer type is part of TextLayersVisualizer; there is no standalone oscilloscope visualizer. Renders a waveform trace; amplitude is scaled by per-layer gain. Color gradient from center (cyan) to edges (red).

## Snapshot usage

- `Waveform` — raw waveform samples
- `WaveformPosition` — current read position (circular buffer)
- `WaveformSize` — buffer size

## Settings

- **Schema**: `TextLayerSettings` when `LayerType == Oscilloscope`
- **Gain** (double, default: 2.5): Amplitude gain (1.0–10.0). Per-layer; adjustable with [ ] when that layer is selected.
- **PaletteId** (string, optional): Id of the color palette for this layer. Inherits from `TextLayers.PaletteId` when null/empty. Use `"oscilloscope"` for the classic gradient (Cyan → Green → Yellow → Red). Press **P** when this layer is selected to cycle and save.

## Key bindings

- **[** — Decrease oscilloscope gain (when Oscilloscope layer is selected in Layered text)
- **]** — Increase oscilloscope gain
- Toolbar suffix (when Oscilloscope layer selected): "Gain: X.X ([ ])"

## Viewport constraints

- Uses full `viewport.Width` × `viewport.Height` (cell buffer); draws trace across width
- No minimum; layer composites with others

## Implementation notes

- **Layer**: `OscilloscopeLayer` in `TextLayers/Oscilloscope/OscilloscopeLayer.cs`
- **Internal state**: None; stateless.
- **Rendering**: Samples waveform across buffer width; draws horizontal line segments between consecutive points; uses `Buffer.Set` for each cell.
- **Color**: Palette-based; maps distance from center (0 = center, 1 = edges) to palette index. Uses the layer's palette (`ctx.Palette` from `PaletteId`). Falls back to the hardcoded gradient (cyan → green → yellow → red) when palette is null/empty. The `oscilloscope.json` palette provides the classic gradient and is available for any layer.
- **References**: [text-layers.md](text-layers.md), [ADR-0014](../adr/0014-visualizers-as-layers.md).
