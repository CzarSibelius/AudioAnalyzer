# Buffer distortion layer

## Blueprint

### Context

- **`BufferDistortion`** (`BufferDistortionLayer`): warps characters and colors already drawn below. Place it **above** the layers you want to affect (higher `ZOrder` = closer to front).
- **Modes** (see `Custom`):
  - **PlaneWaves**: sinusoidal displacement along X and/or Y (ocean-like bands or scan-line motion).
  - **DropRipples**: radial ripples from impact points; new ripples can spawn when the **beat count** advances (optional).
- **Performance**: work is **O(width × height)** of the effect rectangle each frame. Use a smaller **`RenderBounds`** (see [ADR-0058](../../../../docs/adr/0058-layer-render-bounds.md)) on large terminals to stay near the main render budget ([ADR-0067](../../../../docs/adr/0067-60fps-target-and-render-fps-overlay.md)).

### Architecture

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

- **Snapshot then warp**: copies `(char, PaletteColor)` for the effect rectangle into reusable scratch arrays, then for each destination cell computes integer sample offsets, clamps, reads from the snapshot, and `Set`s the buffer. Avoids in-place displacement tearing.
- **State**: `BufferDistortionState` in `ITextLayerStateStore<BufferDistortionState>` holds plane phase, beat latch, and the active ripple list ([ADR-0043](../../../../docs/adr/0043-textlayer-state-store.md)).

### Constraints

None specific; use **S** for settings and **←/→** to select layer type like any other layer.

Inherits the same minimum visualizer size as other text layers. If the computed effect rectangle has zero size, the layer no-ops.

## Contract

### Definition of Done

- Uses **`ViewportCellBuffer.Get` / `Set`** in **buffer coordinates** (full viewport), not `AudioAnalysisSnapshot` pixels.
- **`BeatCount`**: when `SpawnOnBeat` is true in **DropRipples** mode, each time `BeatCount` increases a new ripple is added (after the first-frame latch so startup does not spawn immediately).

### Regression guardrails

- New visual content is a **text layer** (`TextLayerRendererBase`), not a new `IVisualizer` ([ADR-0014](../../../../docs/adr/0014-visualizers-as-layers.md)).
- Viewport rules: .cursor/rules/visualizers-viewport.mdc.

### Scenarios

```gherkin
Scenario: Layer draws when enabled
  Given the layer is present in the active preset with Enabled true
  When TextLayersVisualizer renders a frame
  Then the layer writes cells consistent with its settings and snapshot inputs
```
