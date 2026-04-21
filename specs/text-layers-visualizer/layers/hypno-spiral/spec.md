# Hypno spiral layer (`hypno_spiral`)

## Blueprint

### Context

Full-viewport **log-radius spiral interference**: two sine waves combine angle and `log(radius)` so Moiré-style bands drift across the screen. **Twist** advances each frame by `RevolutionsPerBeat` at the detected **BPM**, so the pattern tends to lock visually to tempo. Output is mapped through a **density charset** (same pattern as Geiss / Fractal zoom).

### Architecture

Common layer fields apply. Custom JSON:

| Property | Type | Default | Description |
| -------- | ---- | ------- | ----------- |
| `ArmCount` | int | 10 | Spiral arms / band count (2–24). |
| `LogPitch` | number | 9 | Tightness of winding in log-radius (2–22). |
| `RevolutionsPerBeat` | number | 1 | Pattern rotations per musical beat at detected BPM (0.125–6). |
| `MoireMix` | number | 0.55 | Blend of a second detuned wave (0–1). |
| `MoirePhase` | number | 1.2 | Fixed phase offset between waves (radians). |
| `MoireDetune` | number | 1.03 | Frequency multiplier on the second wave (0.92–1.08). |
| `MoireDriftSpeed` | number | 0.15 | Slow extra drift on the secondary pattern (rad/s). |
| `BeatReaction` | string enum | `None` | `None`, `SpeedBurst`, `Flash`. |
| `CharsetId` | string | unset | Charset from `charsets/*.json` (ADR-0080); unset → `density-soft`. |

- Renderer: `HypnoSpiralLayer` (`TextLayers/HypnoSpiral/`), `CharsetResolver` + optional `CharsetId`.
- Stateful: `HypnoSpiralState` (`TwistRadians`, `MoireDrift`) in `TextLayerStateStore`.
- Default density literal matches `FractalZoomLayer` when charset resolution fails.

### Constraints

Shared TextLayers bindings only.

Fills the layer-local viewport; works from small terminals upward. Very small viewports (under ~8×4) still render but bands are coarse.

## Contract

### Definition of Done

- `CurrentBpm` — twist speed = `2π × (BPM/60) × RevolutionsPerBeat` (per second), scaled by `SpeedMultiplier`, `SpeedBurst`, and optional `SpeedBurst` beat reaction.
- `BeatFlashActive` — when `BeatReaction` is `Flash`, slightly boosts the brightness mapping; `SpeedBurst` increases twist speed while the beat flash gate is active.
- `FrameDeltaSeconds` — integrates twist and optional moiré drift.

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
