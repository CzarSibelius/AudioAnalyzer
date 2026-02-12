# Winamp Style (winamp)

## Description

Classic Winamp-style spectrum bars. Horizontal bars per frequency band with peak hold markers. Simpler layout than Spectrum Analyzer (no volume bar or labels).

## Snapshot usage

- `SmoothedMagnitudes` — per-band smoothed magnitudes
- `PeakHold` — per-band peak hold for white markers
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization

## Settings

- **Schema**: None (no per-visualizer settings)

## Key bindings

- None mode-specific

## Viewport constraints

- Minimum width: 30
- Minimum height: 5 lines
- Uses `viewport.MaxLines - 3` for bar height (bars + separator + footer)
- Bar height clamped to 10–20
- Number of bars: `min(NumBands, (viewport.Width - 4) / 3)` (each bar 3 chars: 2 for bar + 1 space)
- Total lines: barHeight + 1 (separator) + 1 (footer)

## Implementation notes

- **Internal state**: `StringBuilder` only; stateless.
- **Colors**: Dark green → green → yellow → dark yellow → red (by row position).
- **Peak markers**: White `▀▀` at peak height.
- **Footer**: "Winamp Style - Classic music player visualization".
