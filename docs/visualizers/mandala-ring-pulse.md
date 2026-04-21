# Mandala ring pulse layer (`mandala_ring_pulse`)

## Description

Concentric rings centered in the layer viewport, with radii that **breathe** using a tempo-driven phase (`CurrentBpm`, `PulsesPerBeat`) and optional **spectrum energy** mixed into thickness. **Angular modulation** (cosine lobes) and optional **radial spokes** (`RingAndSpoke`) give a mandala / gate aesthetic. Intended as a translucent overlay on top of fills or Geiss-style backgrounds.

## Snapshot usage

- `CurrentBpm` — drives phase speed (falls back to 120 when BPM is unavailable or out of range).
- `SmoothedMagnitudes`, `TargetMaxMagnitude` — broadband energy mix into ring thickness when `EnergyMix` &gt; 0.
- `BeatFlashActive` — when `BeatReaction` is `SpeedBurst` or `Flash`, boosts pulse speed or adds a one-step palette emphasis.
- `FrameDeltaSeconds` — phase and angular motion integrate in wall time (seconds per frame).

## Settings

Common layer fields apply (`ZOrder`, `SpeedMultiplier`, `PaletteId`, `ColorIndex`, optional `RenderBounds`). Custom JSON (`Custom` object on the layer):

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `Pattern` | string enum | `RingAndSpoke` | `ConcentricRings` or `RingAndSpoke` (rings + radial spokes). |
| `RingCount` | int | 7 | Number of ring bands (3–16). |
| `Symmetry` | int | 8 | Angular lobes and spoke count (3–16). |
| `PulsesPerBeat` | int | 4 | Tempo pulses per beat (1–8); e.g. 4 pulses per quarter note at the detected BPM. |
| `PulseDepth` | number | 0.22 | Ring breathing amplitude (0–0.45). |
| `AngularMotion` | number | 0.35 | Spokes/lobe rotation in radians per second. |
| `EnergyMix` | number | 0.35 | Blend of spectral energy into thickness (0–1). |
| `BeatReaction` | string enum | `None` | `None`, `SpeedBurst`, `Flash`. |

## Key bindings

Uses shared TextLayers bindings (palette **P**, layer select **1–9**, type **←/→**). No layer-specific keys.

## Viewport constraints

Uses full layer-local width and height (honors `RenderBounds` the same way as other full-raster layers). Minimum practical size: about 12×6 cells for readable rings.

## Implementation notes

- Renderer: `MandalaRingPulseLayer` (`TextLayers/MandalaRingPulse/`).
- Stateful: `MandalaRingPulseState` in the shared `TextLayerStateStore` (phase, angular offset, smoothed energy).
- Elliptical distance uses the same vertical aspect compensation as `BeatCirclesLayer` (taller cells are not overstretched radially).
- Characters are chosen from a small intensity ramp (`·░▒`); color cycles through the layer palette by radius and `ColorIndex`.
