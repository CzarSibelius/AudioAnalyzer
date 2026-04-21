# Fractal zoom (layer)

## Blueprint

### Context

Full-viewport **Mandelbrot** or **Julia** escape-time fractal rendered with a density character ramp (similar spirit to GeissBackground). **Zoom** is animated by cycling a zoom phase and drifting the view center on each wrap so the motion does not repeat identically; a slow **rotation** adds variation. **Dwell** modes remap phase so more frames are spent in mid-zoom bands where boundary detail reads better on coarse ASCII. **Smooth escape** shading (fractional iteration) softens banding. This is a **performance-oriented** preview (low iteration cap, double precision only), not mathematically correct deep zoom.

**Illusory infinite zoom** (default **on** via `IllusoryInfiniteZoom`): each time the zoom phase wraps past 1.0, the layer advances a **segment index** and **re-seeds** the apparent view—new orbit anchor in the complex plane and (in Julia mode) small deterministic nudges to the Julia constant—using a fixed catalog and formulas, **not** a continuous path in \(\mathbb{C}\). This keeps motion feeling “endless” without arbitrary-precision math. When **off**, behavior matches the legacy single anchor \((-0.75, 0.05)\) and zero Julia nudge.

### Architecture

- **Schema**: `TextLayerSettings` when `LayerType` is `FractalZoom`; custom settings in `FractalZoomSettings` (`Custom` in JSON).
- **Fractal mode** (`FractalMode`): **Mandelbrot** (c per pixel) or **Julia** (fixed **Julia re** / **Julia im**, z starts at pixel).
- **Julia re** / **Julia im** (-2.0–2.0): Julia constant; ignored in Mandelbrot mode.
- **Max iterations** (4–32): Escape-time cap; higher sharpens boundaries and costs more CPU.
- **Log scale min** / **Log scale max** (negative values): Natural-log scale range passed to `exp` for pixel size. More negative **min** = more zoomed out; less negative **max** = more zoomed in. Defaults **-10** / **-2.6** (same as the original hardcoded range). Tighten the span to spend less time in empty-looking extremes. If **min** and **max** are reversed in the preset, the layer swaps them at runtime.
- **Zoom dwell** (`Dwell`): **Linear** — phase maps 1:1 to scale (legacy behavior). **Mild** / **Strong** — piecewise-linear **plateau**: outer parts of the phase cycle move through scale faster; the **middle** of the cycle advances scale more slowly so interesting structure stays on screen longer.
- **Orbit step** (0.02–0.20 rad): Added to the orbit angle when zoom phase wraps. Smaller steps pan more gently between cycles (default **0.06**, previously fixed **0.12**).
- **Zoom speed** (0.0005–0.02): Multiplier for zoom phase advance per frame (combined with layer **SpeedMultiplier** and **SpeedBurst**).
- **Beat reaction** (`FractalZoomBeatReaction`): **None**, **Flash** (beat brightens mapping), **SpeedBurst** (doubles zoom phase speed while beat flash is active).
- **Illusory infinite zoom** (`IllusoryInfiniteZoom`, bool): When **true** (default), each phase wrap increments **segment index** and applies re-seeded anchor / Julia nudge from `FractalZoomIllusoryReseed`. When **false**, segment stays at 0 and legacy anchor is used.
- **SpeedMultiplier** (common layer property): Scales animation speed; **P** cycles the layer palette.

- `FractalZoomSampler` — integer escape counts plus **smooth** escape (`EscapeSmoothMandelbrot` / `EscapeSmoothJulia`) for shading.
- `FractalZoomAnimation.RemapPhaseToScaleT` — dwell curves for **Mild** / **Strong**.
- `FractalZoomIllusoryReseed` — deterministic anchor \((re, im)\) per segment and Julia nudge pair for Julia mode.
- Per-slot animation state: `FractalZoomState` (`ZoomPhase`, `OrbitAngle`, `ViewRotation`, `SegmentIndex`, `JuliaOffsetRe`, `JuliaOffsetIm`) in `ITextLayerStateStore<FractalZoomState>`.

### Constraints

None layer-specific. Use **S** for settings, **←/→** to change layer type, **1–9** to select a layer.

Fills the layer’s **Render region** when set; otherwise the full visualizer viewport. **ZOrder** is respected with other layers.

## Contract

### Definition of Done

- **`BeatFlashActive`** — Used when **Beat reaction** is **Flash** (brighter normalization) or **SpeedBurst** (faster zoom phase advance).
- **`IllusoryInfiniteZoom`** — When enabled, phase wraps advance `SegmentIndex` and change re-seeded parameters; when disabled, anchors and Julia offsets match legacy defaults for every frame.

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

```gherkin
Scenario: Illusory re-seed advances across wraps
  Given FractalZoom has IllusoryInfiniteZoom true
  And zoom phase is advanced until it wraps at least twice
  When each wrap is processed
  Then SegmentIndex increases by one per wrap
  And successive anchors from FractalZoomIllusoryReseed differ by at least 1e-4 in the complex plane metric
```

```gherkin
Scenario: Viewport is not exceeded
  Given the layer is enabled with a non-maximal render region
  When FractalZoomLayer draws
  Then only in-bounds cells for that region are written
```
