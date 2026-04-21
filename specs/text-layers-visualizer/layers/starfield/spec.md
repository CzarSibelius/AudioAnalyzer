# Starfield (layer)

## Blueprint

### Context

**Pseudo-3D starfield**: many points in a simple depth volume move toward the viewer; each frame they are **perspective-projected** to terminal cells, sorted **far-to-near** for occlusion, and drawn as single characters from a **charset** ramp (classic “fly through space” feel). See [ADR-0082](../../../../docs/adr/0082-starfield-text-layer.md).

### Architecture

- **Schema**: `TextLayerSettings` when `LayerType` is `Starfield`; custom settings in `StarfieldSettings` (`Custom` in JSON).
- **Star count** (20–600 in S modal; **hard-clamped to 1000** in code if JSON exceeds the cap).
- **Base speed** (0.1–4.0): Travel speed scale (combined with common **SpeedMultiplier** and **SpeedBurst**).
- **Travel seconds** (12–120, default **32**): Approximate wall time to move through the full Z range at **Base speed** 1.0 (larger = slower, calmer cruise). Clamped in code to **4–180** if JSON is out of range.
- **Center drift X / Y** (−12…12 cells/s): Accumulating offset of the projection center (vanishing point drift).
- **View offset X / Y** (−40…40 cells): Static center offset (not accumulated).
- **Tumble rad/s** (−0.4…0.4): Slow incremental rotation of star X/Y in model space.
- **Spread X / Y** (0.5–8): Half-extent of spawn volume in model units.
- **Focal length** (4–120): Perspective scale (larger = wider spread on screen).
- **Z near** / **Z far**: Depth range; stars (re)spawn with **random Z** between **Z near** and **Z far** so near and far stars coexist; when **Z** crosses **Z near**, the star respawns with a new draw in that slab.
- **Cell aspect** (1–3, default **2**): Corrects non-square console cells in the vertical projection (same role as other layers’ aspect tweaks).
- **Depth shading** (`Flat` | `DepthGradient`): **Flat** uses `ColorIndex` only; **DepthGradient** walks the palette by depth.
- **Beat reaction** (`None` | `SpeedBurst` | `Flash`): See Description.
- **Fixed seed** (−1 or 0…999999): **−1** uses `Random.Shared` for spawns; **≥ 0** uses a dedicated `Random(seed)`. On full field reinit (resize, count change, or seed change), the fixed RNG is **recreated** so the first spawn sequence is repeatable.
- **Charset** (`CharsetId`): Glyph pool for stars; unset uses **`density-soft`** with a built-in ASCII fallback if the file is missing.

Common layer properties: **SpeedMultiplier**, **ColorIndex**, **PaletteId**, optional **RenderBounds**.

- `StarfieldProjection.Project` — shared perspective math (unit-tested).
- `StarfieldLayerState` — `Stars[]`, `SortScratch[]`, drift accumulators, `RecreateFixedRandom` for deterministic respawn after Ctrl+R–style clears.
- `StarfieldLayer` — `ITextLayerStateStore<StarfieldLayerState>`, `CharsetResolver`.

### Constraints

None layer-specific. **S** for settings, **←/→** to change layer type, **1–9** to select a layer, **P** to cycle palette for the active layer.

Respects **RenderBounds** (layer-local width/height and buffer origin). Only stars whose projected cell lies inside the layer rectangle are drawn.

## Contract

### Definition of Done

- **`BeatFlashActive`** — Used when **Beat reaction** is **SpeedBurst** (faster travel) or **Flash** (palette index shift while active).
- **`FrameDeltaSeconds`** — Scaled with `DisplayAnimationTiming.ScaleForReference60` ([ADR-0072](../../../../docs/adr/0072-delta-time-display-animation.md)).

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
