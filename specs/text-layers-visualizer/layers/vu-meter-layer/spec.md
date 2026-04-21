# VU Meter (layer)

## Blueprint

### Context

Classic stereo VU-style meters. Shows left and right channel levels with peak hold markers, dB scale, and a balance indicator (L–C–R). This layer type is part of TextLayersVisualizer; there is no standalone VU meter visualizer.

### Architecture

- Uses shared `TextLayerSettings`; no layer-specific options.

- **Stateless**: No per-frame state; draws into cell buffer.
- **Colors**: Green (0–75%), yellow (75–90%), red (90–100%); white peak marker.
- **dB display**: `20 * log10(level)` for each channel.
- **Balance**: `(R - L) / (L + R)` mapped to L–C–R bar.
- **Location**: `TextLayers/VuMeter/VuMeterLayer.cs`

### Constraints

- None layer-specific

- Minimum width: 30
- Minimum height: 7 lines
- Meter width: `min(60, viewport.Width - 20)`

## Contract

### Definition of Done

- `LeftChannel` — left channel level (0–1)
- `RightChannel` — right channel level (0–1)
- `LeftPeakHold` — left peak hold position
- `RightPeakHold` — right peak hold position

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
