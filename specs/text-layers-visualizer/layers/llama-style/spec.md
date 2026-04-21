# Llama Style (layer)

## Blueprint

### Context

Classic spectrum bars (formerly Winamp Style). Horizontal bars per frequency band with peak hold markers. This layer type is part of TextLayersVisualizer; there is no standalone spectrum visualizer. Supports configurable options: volume bar, row labels, frequency labels, color scheme, peak marker style, and bar density.

### Architecture

- **Schema**: `TextLayerSettings` when `LayerType == LlamaStyle`; custom settings in `LlamaStyleSettings`. Property names in Custom JSON match C# (e.g. `ShowVolumeBar`).
- **ShowVolumeBar** (bool, default false): Show volume bar at top.
- **ShowRowLabels** (bool, default false): Show percentage labels (100%, 75%, 50%, 25%, 0%) on left.
- **ShowFrequencyLabels** (bool, default false): Show Hz labels at bottom.
- **ColorScheme** (string, default "Winamp"): "Winamp" (green→red) or "Spectrum" (red→blue).
- **PeakMarkerStyle** (string, default "Blocks"): "Blocks" (▀▀) or "DoubleLine" (══).
- **BarWidth** (int, default 3): 2 or 3 chars per band (2 = denser).

- **Stateless**: No per-frame state; draws into cell buffer.
- **Default**: Winamp-style look (no volume bar, no labels, green→red, ▀▀ peak).
- **Spectrum-style**: Enable all options + ColorScheme "Spectrum" + PeakMarkerStyle "DoubleLine" + BarWidth 2.
- **Location**: `TextLayers/LlamaStyle/LlamaStyleLayer.cs`

### Constraints

- None layer-specific

- Minimum width: 30
- Minimum height: 5 lines
- Bar height: 10–30 (when Spectrum-style options enabled), 10–20 (Winamp-style)

## Contract

### Definition of Done

- `SmoothedMagnitudes` — per-band smoothed magnitudes
- `PeakHold` — per-band peak hold for markers
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization
- `Volume` — overall volume (when ShowVolumeBar enabled)

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
