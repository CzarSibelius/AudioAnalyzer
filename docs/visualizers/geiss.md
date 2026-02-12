# Geiss (geiss)

## Description

Psychedelic plasma-style visualization driven by spectrum magnitudes. Bass and treble intensity modulate the plasma; optional expanding beat circles spawn on beat detection. Uses the selected color palette when available.

## Snapshot usage

- `SmoothedMagnitudes` — used to compute bass (first quarter) and treble (last quarter) intensity
- `TargetMaxMagnitude` — gain for magnitude normalization
- `Palette` — optional; when present, plasma and beat circles use palette colors
- `BeatCount` — triggers new beat circle when it changes
- `ShowBeatCircles` — when true, beat circles are drawn and spawned
- `BeatFlashActive` — boosts plasma intensity briefly on beat

## Settings

- **Schema**: `VisualizerSettings.Geiss`
- **BeatCircles** (bool, default: true): Show expanding circles on beat.
- **Related**: `SelectedPaletteId` — palette used when `Palette` is set by the renderer.

## Key bindings

- **B** — Toggle beat circles (global, affects Geiss when in Geiss mode)
- **P** — Cycle color palette (when in Geiss mode)

## Viewport constraints

- Minimum width: 30
- Minimum height: 3 lines
- Uses `viewport.MaxLines - 2` for plasma height (one blank line, one footer); height clamped to 12–25.
- Uses `viewport.Width - 4` for plasma width (2-char margin each side); width clamped to max 100.
- Footer line: "Geiss - Psychedelic | Bass: X | Treble: Y"
- Total lines written must not exceed `viewport.MaxLines`.

## Implementation notes

- **Internal state**: `_phase`, `_colorPhase` (animation); `_bassIntensity`, `_trebleIntensity` (smoothed); `_beatCircles` (list of `BeatCircle`); `_lastBeatCount`.
- **Beat circles**: Up to 5 circles; each expands from 0.02 to `maxRadius` (0.3–0.7 based on bass); removed when radius exceeds max or age > 30.
- **Plasma algorithm**: Sine-based plasma with bass/treble modulation; character gradient ` .,'-_"/~*#`; hue for palette index.
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md).
