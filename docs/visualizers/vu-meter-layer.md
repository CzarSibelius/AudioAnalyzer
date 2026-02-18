# VU Meter (layer)

## Description

Classic stereo VU-style meters. Shows left and right channel levels with peak hold markers, dB scale, and a balance indicator (L–C–R). This layer type is part of TextLayersVisualizer; there is no standalone VU meter visualizer.

## Snapshot usage

- `LeftChannel` — left channel level (0–1)
- `RightChannel` — right channel level (0–1)
- `LeftPeakHold` — left peak hold position
- `RightPeakHold` — right peak hold position

## Settings

- Uses shared `TextLayerSettings`; no layer-specific options.

## Key bindings

- None layer-specific

## Viewport constraints

- Minimum width: 30
- Minimum height: 7 lines
- Meter width: `min(60, viewport.Width - 20)`

## Implementation notes

- **Stateless**: No per-frame state; draws into cell buffer.
- **Colors**: Green (0–75%), yellow (75–90%), red (90–100%); white peak marker.
- **dB display**: `20 * log10(level)` for each channel.
- **Balance**: `(R - L) / (L + R)` mapped to L–C–R bar.
- **Location**: `TextLayers/VuMeter/VuMeterLayer.cs`
