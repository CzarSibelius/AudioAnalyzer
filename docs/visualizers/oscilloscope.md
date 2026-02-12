# Oscilloscope (oscilloscope)

## Description

Time-domain waveform display showing audio amplitude over time. Renders a 2D grid with center line; waveform amplitude is scaled by user-adjustable gain. Color gradient from center (cyan) to edges (red).

## Snapshot usage

- `Waveform` — raw waveform samples
- `WaveformPosition` — current read position (circular buffer)
- `WaveformSize` — buffer size
- `WaveformGain` — amplitude gain (1.0–10.0), adjustable in real time

## Settings

- **Schema**: `VisualizerSettings.Oscilloscope`
- **Gain** (double, default: 2.5): Amplitude gain (1.0–10.0). Persisted and used for `WaveformGain` in snapshot.

## Key bindings

- **[** — Decrease oscilloscope gain
- **]** — Increase oscilloscope gain
- Toolbar suffix: "Gain: X.X ([ ])"

## Viewport constraints

- Minimum width: 30
- Minimum height: 5 lines
- Uses `viewport.MaxLines - 4` for grid height (top border, grid, bottom border, footer)
- Grid height clamped to 10–25
- Width: `viewport.Width - 4` (2-char margin each side); clamped to `WaveformSize`
- Total lines: 1 (top border) + height + 1 (bottom border) + 1 (footer)

## Implementation notes

- **Internal state**: None beyond `StringBuilder`; stateless except for line buffer.
- **Rendering**: Fills a 2D `char`/`ConsoleColor` grid; draws horizontal line segments between consecutive waveform points.
- **Color**: Distance from center determines color (cyan → green → yellow → red).
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md).
