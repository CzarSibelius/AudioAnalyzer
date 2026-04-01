# Fractal zoom (layer)

## Description

Full-viewport **Mandelbrot** or **Julia** escape-time fractal rendered with a density character ramp (similar spirit to GeissBackground). **Zoom** is animated by cycling a zoom phase and drifting the view center on each wrap so the motion does not repeat identically; a slow **rotation** adds variation. **Dwell** modes remap phase so more frames are spent in mid-zoom bands where boundary detail reads better on coarse ASCII. **Smooth escape** shading (fractional iteration) softens banding. This is a **performance-oriented** preview (low iteration cap, double precision only), not mathematically correct deep zoom.

## Snapshot usage

- **`BeatFlashActive`** — Used when **Beat reaction** is **Flash** (brighter normalization) or **SpeedBurst** (faster zoom phase advance).

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `FractalZoom`; custom settings in `FractalZoomSettings` (`Custom` in JSON).
- **Fractal mode** (`FractalMode`): **Mandelbrot** (c per pixel) or **Julia** (fixed **Julia re** / **Julia im**, z starts at pixel).
- **Julia re** / **Julia im** (-2.0–2.0): Julia constant; ignored in Mandelbrot mode.
- **Max iterations** (4–32): Escape-time cap; higher sharpens boundaries and costs more CPU.
- **Log scale min** / **Log scale max** (negative values): Natural-log scale range passed to `exp` for pixel size. More negative **min** = more zoomed out; less negative **max** = more zoomed in. Defaults **-10** / **-2.6** (same as the original hardcoded range). Tighten the span to spend less time in empty-looking extremes. If **min** and **max** are reversed in the preset, the layer swaps them at runtime.
- **Zoom dwell** (`Dwell`): **Linear** — phase maps 1:1 to scale (legacy behavior). **Mild** / **Strong** — piecewise-linear **plateau**: outer parts of the phase cycle move through scale faster; the **middle** of the cycle advances scale more slowly so interesting structure stays on screen longer.
- **Orbit step** (0.02–0.20 rad): Added to the orbit angle when zoom phase wraps. Smaller steps pan more gently between cycles (default **0.06**, previously fixed **0.12**).
- **Zoom speed** (0.0005–0.02): Multiplier for zoom phase advance per frame (combined with layer **SpeedMultiplier** and **SpeedBurst**).
- **Beat reaction** (`FractalZoomBeatReaction`): **None**, **Flash** (beat brightens mapping), **SpeedBurst** (doubles zoom phase speed while beat flash is active).
- **SpeedMultiplier** (common layer property): Scales animation speed; **P** cycles the layer palette.

## Key bindings

None layer-specific. Use **S** for settings, **←/→** to change layer type, **1–9** to select a layer.

## Viewport constraints

Fills the layer’s **Render region** when set; otherwise the full visualizer viewport. **ZOrder** is respected with other layers.

## Implementation notes

- `FractalZoomSampler` — integer escape counts plus **smooth** escape (`EscapeSmoothMandelbrot` / `EscapeSmoothJulia`) for shading.
- `FractalZoomAnimation.RemapPhaseToScaleT` — dwell curves for **Mild** / **Strong**.
- Per-slot animation state: `FractalZoomState` (`ZoomPhase`, `OrbitAngle`, `ViewRotation`) in `ITextLayerStateStore<FractalZoomState>`.
