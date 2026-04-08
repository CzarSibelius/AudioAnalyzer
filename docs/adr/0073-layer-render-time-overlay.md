# ADR-0073: Per-layer render time in settings modal and UI setting

**Status**: Accepted

## Context

Developers tuning text layers need to see **how long each layer’s `Draw` call takes** without external profilers. The preset / layer settings modal (S) lists layers by type in the left column; that is the natural place to show timings next to each layer name.

`AudioAnalysisSnapshot` is produced by `AnalysisEngine.GetSnapshot()` for analysis data only. The visualization orchestrator builds a **`VisualizationFrameContext`** each frame (analysis + layout + `FrameDeltaSeconds` + optional FPS) and passes it to the renderer; `TextLayersVisualizer` may set `LayerRenderTimeMs` on that context. The settings modal uses `GetFrameForUi()`, which merges fresh analysis with a **cached copy** of the last layer timings so the modal never mutates the cache during the next frame.

## Decision

1. **UiSettings**: Add `ShowLayerRenderTime` (bool, default `false`), persisted in `appsettings.json` like `ShowRenderFps`. Toggled from **General settings** hub (Enter on the row).

2. **Measurement**: When `ShowLayerRenderTime` is true, `TextLayersVisualizer` measures **only** each executed `renderer.Draw` using `Stopwatch.GetTimestamp()` and frequency (ADR-0030, ADR-0067). When false, **no** per-layer timing overhead.

3. **Frame field**: `VisualizationFrameContext` exposes `LayerRenderTimeMs`: `double?[]?` with length `TextLayersLimits.MaxLayerCount`, index = **sorted layer index**. `null` at an index means that layer was not timed this frame (disabled, missing renderer, or setting off). Values are **display/render pipeline** data, not from the analysis engine (ADR-0024).

4. **Orchestrator cache**: After each successful `_renderer.Render(frame)`, copy `LayerRenderTimeMs` into an orchestrator-held buffer when the setting is on. `GetFrameForUi()` returns a new `VisualizationFrameContext` whose `Analysis` comes from `GetSnapshot()` on the engine and **merges** a **copy** of that cached array into the result so the modal never mutates the cache during the next frame.

5. **UI**: When the setting is on, the S modal left column appends a compact timing suffix per layer (e.g. milliseconds); use `—` when the entry is null. Truncate to the fixed left column width (ellipsis / display width per ADR-0039). The suffix uses **effective UI palette** colors by tier vs a **fair-share budget**: frame time at 60 FPS (`1000/60` ms) divided by **enabled** layer count; over 100% of that budget → `Highlighted`, between 25% and 100% → `Normal`, at or below 25% (and em dash when not timed) → `Dimmed`. Selected layer rows use plain text so selection highlight stays consistent.

6. **Toolbar**: Not in scope: the toolbar is drawn before the visualizer in Preset editor, so same-frame timings would require staging; only the modal shows timings via `GetFrameForUi()`.

## Consequences

- Layer timings reflect **last completed main render** when read from `GetFrameForUi()`, which is sufficient for debugging.
- Turning the feature off avoids timer work on the hot path.
- New hub row and JSON property must be documented in configuration reference and UI specs.
