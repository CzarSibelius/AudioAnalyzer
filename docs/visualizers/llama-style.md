# Llama Style (layer)

## Description

Classic spectrum bars (formerly Winamp Style). Horizontal bars per frequency band with peak hold markers. Supports configurable options from the former Spectrum Analyzer: volume bar, row labels, frequency labels, color scheme, peak marker style, and bar density.

## Snapshot usage

- `SmoothedMagnitudes` — per-band smoothed magnitudes
- `PeakHold` — per-band peak hold for markers
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization
- `Volume` — overall volume (when ShowVolumeBar enabled)

## Settings

- **LlamaStyleShowVolumeBar** (bool, default false): Show volume bar at top.
- **LlamaStyleShowRowLabels** (bool, default false): Show percentage labels (100%, 75%, 50%, 25%, 0%) on left.
- **LlamaStyleShowFrequencyLabels** (bool, default false): Show Hz labels at bottom.
- **LlamaStyleColorScheme** (string, default "Winamp"): "Winamp" (green→red) or "Spectrum" (red→blue).
- **LlamaStylePeakMarkerStyle** (string, default "Blocks"): "Blocks" (▀▀) or "DoubleLine" (══).
- **LlamaStyleBarWidth** (int, default 3): 2 or 3 chars per band (2 = denser).

## Key bindings

- None layer-specific

## Viewport constraints

- Minimum width: 30
- Minimum height: 5 lines
- Bar height: 10–30 (when Spectrum-style options enabled), 10–20 (Winamp-style)

## Implementation notes

- **Stateless**: No per-frame state; draws into cell buffer.
- **Default**: Winamp-style look (no volume bar, no labels, green→red, ▀▀ peak).
- **Spectrum-style**: Enable all options + ColorScheme "Spectrum" + PeakMarkerStyle "DoubleLine" + BarWidth 2.
- **Location**: `TextLayers/LlamaStyle/LlamaStyleLayer.cs`
