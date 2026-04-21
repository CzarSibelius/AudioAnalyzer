# Oscilloscope (layer only)

## Blueprint

### Context

Time-domain waveform display showing audio amplitude over time. This layer type is part of TextLayersVisualizer; there is no standalone oscilloscope visualizer. Renders a waveform trace; amplitude is scaled by per-layer gain. Color gradient from center (cyan) to edges (red).

### Architecture

- **Schema**: `TextLayerSettings` when `LayerType == Oscilloscope`
- **Gain** (double, default: 2.5): Amplitude gain (1.0–10.0). Per-layer; adjustable with [ ] when that layer is selected.
- **Filled** (bool, default: false): When true, fill the area between center line and waveform; when false, draw only the trace. Editable in the S modal.
- **PaletteId** (string, optional): Id of the color palette for this layer. Inherits from `TextLayers.PaletteId` when null/empty. Use `"oscilloscope"` for the classic gradient (Cyan → Green → Yellow → Red). Press **P** when this layer is selected to cycle and save.

- **Layer**: `OscilloscopeLayer` in `TextLayers/Oscilloscope/OscilloscopeLayer.cs`
- **Internal state**: None; stateless.
- **Rendering**: Samples waveform across buffer width; draws vertical segments. When Filled is false, draws the trace only (segment between consecutive points); when Filled is true, fills from center line to waveform for each column. Uses `Buffer.Set` for each cell.
- **Color**: Palette-based; maps distance from center (0 = center, 1 = edges) to palette index. Uses the layer's palette (`ctx.Palette` from `PaletteId`). Falls back to the hardcoded gradient (cyan → green → yellow → red) when palette is null/empty. The `oscilloscope.json` palette provides the classic gradient and is available for any layer.
- **References**: [TextLayers hub](../../spec.md), [ADR-0014](../../../../docs/adr/0014-visualizers-as-layers.md).

### Constraints

- **[** — Decrease oscilloscope gain (when Oscilloscope layer is selected in Layered text)
- **]** — Increase oscilloscope gain
- Toolbar (when Oscilloscope is the palette-cycled layer): contextual **Gain** value; bracket keys **[ ]** adjust gain while that layer is selected

- Uses full `viewport.Width` × `viewport.Height` (cell buffer); draws trace across width
- No minimum; layer composites with others

## Contract

### Definition of Done

- `Waveform` — raw waveform samples
- `WaveformPosition` — current read position (circular buffer)
- `WaveformSize` — buffer size

### Regression guardrails

- New visual content is a **text layer** (`TextLayerRendererBase`), not a new `IVisualizer` ([ADR-0014](../../../../docs/adr/0014-visualizers-as-layers.md)).
- Viewport rules: .cursor/rules/visualizers-viewport.mdc.

### Scenarios

```gherkin
Scenario: Layer draws when enabled
  Given the layer is present in the active preset with Enabled true
  When TextLayersVisualizer renders a frame
  Then the layer writes cells consistent with its settings and snapshot inputs
```
