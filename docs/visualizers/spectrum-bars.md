# Spectrum Analyzer (spectrum)

## Description

Volume bar plus frequency spectrum bars with peak hold markers. Shows 0–100% volume, frequency bands with amplitude-based colors, and Hz labels.

## Snapshot usage

- `Volume` — overall volume for top bar
- `SmoothedMagnitudes` — per-band smoothed magnitudes
- `PeakHold` — per-band peak hold for white markers
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization

## Settings

- **Schema**: None (no per-visualizer settings)
- Uses global analysis parameters (smoothing, peak hold, etc.)

## Key bindings

- None mode-specific

## Viewport constraints

- Minimum width: 30
- Minimum height: 5 lines
- Uses `viewport.MaxLines - 6` for bar height (volume bar + blank + bars + separator + labels)
- Bar height clamped to 10–30
- Number of bands: `(viewport.Width - 5) / 2` (each bar 2 chars)
- Total lines: 2 (volume) + barHeight + 1 (separator) + 2 (labels)

## Implementation notes

- **Internal state**: `StringBuilder` only; stateless.
- **Volume bar**: Amplitude-based gradient (blue → cyan → green → yellow → magenta → red).
- **Bars**: Each band is 2 chars; color by row position (red at bottom, blue at top); white peak markers.
- **Labels**: Fixed Hz labels (20, 30, 50, … 20k); interval based on band count.
