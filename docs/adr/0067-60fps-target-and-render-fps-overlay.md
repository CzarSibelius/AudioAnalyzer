# ADR-0067: 60 FPS target and on-screen main render FPS

**Status**: Accepted

## Context

The console visualizer historically throttled full main-area redraws to about 20 Hz (50 ms) for CPU and console I/O cost (see ADR-0030). Users and developers need a **stated performance target** and a way to see **actual full-frame rate** without external profilers. Header-only refresh (ADR-0038) uses a separate timer so scrolling metadata can animate when loopback is silent; that rate must not be confused with visualization FPS.

## Decision

1. **Target frame rate**: Aim for **~60 FPS** for the **full main visualization frame**—the path that runs `IVisualizationRenderer.Render` and composes the toolbar plus visualizer (or General Settings hub) via `MainContentContainer`. This is not a hard real-time guarantee: heavy layers, slow hosts, or capture cadence may reduce achieved FPS.

2. **Definition of measured FPS**: **Completed successful full main renders per second**, using intervals between successive successful `_renderer.Render` completions in `VisualizationOrchestrator`. **Header-only** updates (`RefreshHeaderIfNeeded`) **do not** advance this counter. The header timer may remain at a lower cadence (e.g. 50 ms) to limit console work on that path unless profiling justifies aligning it with 60 Hz.

3. **Measurement mechanics**: Use **`Stopwatch.GetTimestamp()`** (or equivalent) for intervals, not `DateTime.Now` (ADR-0030). Maintain a **rolling average** over the last **45** completed frame intervals for a stable readout. Expose the current smoothed value on **`AnalysisSnapshot.MeasuredMainRenderFps`** when the overlay is enabled; otherwise leave it unset (`null`).

4. **On-screen overlay**: When enabled, show a compact **`FPS:value`** cell on the **main toolbar row** in Preset editor, Show play, and General settings (modes that use `MainContentToolbarLayout`). In **fullscreen** layout where the toolbar is omitted, no FPS cell is shown. **Modals**: while the render guard suppresses full redraws, the last displayed value may freeze; no overlay inside modals is required.

5. **Toggle**: **`UiSettings.ShowRenderFps`** (persisted in appsettings with other UI settings). A **General Settings hub** row toggles the value with **Enter** and persists via existing save flow.

## Consequences

- **Throttle alignment**: Main-loop sleep and orchestrator audio-driven throttle target **~16 ms** (~60 Hz) when the pipeline can keep up. WASAPI callback rate may still cap observed FPS.
- **CPU / I/O**: Higher peak load than 50 ms pacing; diff-based `ViewportCellBuffer` remains important (ADR-0030).
- **Cross-references**: Supersedes the implicit “~20 Hz” as the **design target** for main redraws (ADR-0030 context); performance rules in ADR-0030 remain. Header independence remains per ADR-0038.
- **Tests**: Integration render budget messaging should reflect the 60 FPS target; unit tests cover FPS rolling-average math on `MainRenderFpsMeter`.

## References

- [`VisualizationOrchestrator`](../../src/AudioAnalyzer.Console/VisualizationOrchestrator.cs) — throttle, full render, FPS sampling
- [`MainContentToolbarLayout`](../../src/AudioAnalyzer.Console/MainContentToolbarLayout.cs) — toolbar FPS cell
- [`MainRenderFpsMeter`](../../src/AudioAnalyzer.Application/Display/MainRenderFpsMeter.cs) — rolling average
