# Buffer distortion layer

Post-process text layer that **reads the current cell buffer** (the composite of all layers with lower Z-order), **snapshots** a rectangle, then **writes** the same region with **displaced sampling** — same idea as [Mirror](mirror.md), extended to wave-like motion.

## Description

- **`BufferDistortion`** (`BufferDistortionLayer`): warps characters and colors already drawn below. Place it **above** the layers you want to affect (higher `ZOrder` = closer to front).
- **Modes** (see `Custom`):
  - **PlaneWaves**: sinusoidal displacement along X and/or Y (ocean-like bands or scan-line motion).
  - **DropRipples**: radial ripples from impact points; new ripples can spawn when the **beat count** advances (optional).
- **Performance**: work is **O(width × height)** of the effect rectangle each frame. Use a smaller **`RenderBounds`** (see [ADR-0058](../adr/0058-layer-render-bounds.md)) on large terminals to stay near the main render budget ([ADR-0067](../adr/0067-60fps-target-and-render-fps-overlay.md)).

## Snapshot usage

- Uses **`ViewportCellBuffer.Get` / `Set`** in **buffer coordinates** (full viewport), not `AudioAnalysisSnapshot` pixels.
- **`BeatCount`**: when `SpawnOnBeat` is true in **DropRipples** mode, each time `BeatCount` increases a new ripple is added (after the first-frame latch so startup does not spawn immediately).

## Settings

All options live in the layer’s **`Custom`** object (`BufferDistortionSettings`):

| Property | Role |
|----------|------|
| `Mode` | `PlaneWaves` or `DropRipples` |
| `PlaneOrientation` | `WaveAlongX`, `WaveAlongY`, or `Both` (plane mode) |
| `PlaneAmplitudeCells` | Peak displacement in cells (0–6) |
| `PlaneWavelengthCells` | Wavelength along the varying axis (cells) |
| `PlanePhaseSpeed` | Phase advance per second (radians); 0 freezes plane phase |
| `SpawnOnBeat` | Drop mode: spawn on beat count advance |
| `MaxRipples` | Cap on concurrent ripples |
| `RippleWaveNumber`, `RippleTimeSpeed`, `RippleAmplitudeCells`, `RippleDecayPerSecond`, `RippleMaxAgeSeconds` | Drop mode: ring spacing, motion, strength, decay |
| `MaxDisplacementCells` | Clamp on combined displacement vector |

**`SpeedMultiplier`** on the layer scales plane phase advance (and is passed through the shared timing path with `FrameDeltaSeconds`).

## Key bindings

None specific; use **S** for settings and **←/→** to select layer type like any other layer.

## Viewport constraints

Inherits the same minimum visualizer size as other text layers. If the computed effect rectangle has zero size, the layer no-ops.

## Implementation notes

- **Snapshot then warp**: copies `(char, PaletteColor)` for the effect rectangle into reusable scratch arrays, then for each destination cell computes integer sample offsets, clamps, reads from the snapshot, and `Set`s the buffer. Avoids in-place displacement tearing.
- **State**: `BufferDistortionState` in `ITextLayerStateStore<BufferDistortionState>` holds plane phase, beat latch, and the active ripple list ([ADR-0043](../adr/0043-textlayer-state-store.md)).
