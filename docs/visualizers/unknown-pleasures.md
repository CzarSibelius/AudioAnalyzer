# Unknown Pleasures (unknownpleasures)

## Description

Multiple stacked waveform snapshots inspired by the pulsar plot. The bottom line is always realtime; the others are beat-triggered frozen snapshots. Gaps between each pulse. Uses the selected color palette.

## Snapshot usage

- `SmoothedMagnitudes` — spectrum magnitudes for pulse lines
- `NumBands` — number of frequency bands
- `TargetMaxMagnitude` — gain for magnitude normalization
- `BeatCount` — triggers a new frozen snapshot when it changes
- `Palette` — required; colors for each pulse row (gaps between pulses)

## Settings

- **Schema**: `VisualizerSettings.UnknownPleasures`
- **PaletteId** (string, optional): Id of the selected color palette (e.g. `"default"`). Resolved from palettes directory. P key cycles and saves to this setting.
- **Palette** (legacy): `ColorPalette` with `ColorNames` — fallback if `PaletteId` not set.

## Key bindings

- **P** — Cycle color palette (affects only Unknown Pleasures; saved to its settings)

## Viewport constraints

- Minimum width: 20
- Minimum height: 5 lines
- Uses `viewport.MaxLines - 1` for available rows
- Each snapshot block: 3 pulse lines + 1 gap line (4 rows per snapshot)
- Bottom block uses live data; others use frozen snapshots
- Line length: `viewport.Width` (full width)
- Total lines written must not exceed `viewport.MaxLines`.

## Implementation notes

- **Internal state**: `_snapshots` (up to 14 `double[SnapshotWidth]`); `_livePulse`; `_lastBeatCount`; `_colorOffset` (rotates on beat).
- **Snapshot width**: Fixed 120 samples; mapped to viewport width when rendering.
- **ASCII gradient**: ` .,'-_\"/~*#` — maps normalized magnitude to character density across three line bands (top/mid/bottom third of magnitude range).
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md).
