# VU Meter (vumeter)

## Description

Classic stereo VU-style meters. Shows left and right channel levels with peak hold markers, dB scale, and a balance indicator (L–C–R).

## Snapshot usage

- `LeftChannel` — left channel level (0–1)
- `RightChannel` — right channel level (0–1)
- `LeftPeakHold` — left peak hold position
- `RightPeakHold` — right peak hold position

## Settings

- **Schema**: None (no per-visualizer settings)

## Key bindings

- None mode-specific

## Viewport constraints

- Minimum width: 30
- Minimum height: 7 lines
- Meter width: `min(60, viewport.Width - 20)`
- Uses `WriteLineSafe` to cap output at `viewport.MaxLines` lines
- Line length: `VisualizerViewport.TruncateToWidth` for non-ANSI lines

## Implementation notes

- **Internal state**: `StringBuilder` only; stateless.
- **Colors**: Green (0–75%), yellow (75–90%), red (90–100%); white peak marker.
- **dB display**: `20 * log10(level)` for each channel.
- **Balance**: `(R - L) / (L + R)` mapped to L–C–R bar.
