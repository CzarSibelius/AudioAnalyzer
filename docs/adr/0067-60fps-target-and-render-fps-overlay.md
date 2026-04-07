# ADR-0067: At least ~60 FPS bar and on-screen main render FPS

**Status**: Accepted

## Context

The console visualizer historically throttled full main-area redraws to about 20 Hz (50 ms) for CPU and console I/O cost (see ADR-0030). Users and developers need a **stated performance target** and a way to see **actual full-frame rate** without external profilers. Header-only refresh (ADR-0038) uses a separate timer so scrolling metadata can animate when loopback is silent; that rate must not be confused with visualization FPS.

**Update (current):** Full main-area redraws are driven by the **`ApplicationShell` main loop**, not by the audio `DataAvailable` callback. The loop calls `Redraw` each iteration and uses **`Thread.Sleep(0)`** as a cooperative yield so the thread does not busy-spin; it does **not** impose a fixed ~16 ms delay, so achieved FPS may **exceed** ~60 on fast hosts. Analysis (`ProcessAudio`) still runs on the capture thread; spectrum/FFT data may update at the device’s buffer cadence even though layers and the toolbar repaint every display tick.

## Decision

1. **Minimum performance bar (~60 FPS)**: Design, optimize, and test so that on **typical capable hardware**, the **full main visualization frame**—the path that runs `IVisualizationRenderer.Render` and composes the toolbar plus visualizer (or General Settings hub) via `MainContentContainer`—can sustain **at least ~60 FPS** when workload allows. This is **not** a hard ceiling: the shell does not cap the loop at ~60 Hz. It is **not** a hard real-time guarantee either: heavy layers or slow hosts may still reduce achieved FPS below that bar. Audio callback rate does **not** cap this render path.

2. **Definition of measured FPS**: **Completed successful full main renders per second**, using intervals between successive successful `_renderer.Render` completions in `VisualizationOrchestrator`. **Header-only** updates (`RefreshHeaderIfNeeded`) **do not** advance this counter. The header timer may remain at a lower cadence (e.g. 50 ms) to limit console work on that path unless profiling justifies aligning it with the main loop.

3. **Measurement mechanics**: Use **`Stopwatch.GetTimestamp()`** (or equivalent) for intervals, not `DateTime.Now` (ADR-0030). Maintain a **rolling average** over the last **45** completed frame intervals for a stable readout. Expose the current smoothed value on **`AnalysisSnapshot.MeasuredMainRenderFps`** when the overlay is enabled; otherwise leave it unset (`null`).

4. **On-screen overlay**: When enabled, show a compact **`FPS:value`** cell on the **main toolbar row** in Preset editor, Show play, and General settings (modes that use `MainContentToolbarLayout`). In **fullscreen** layout where the toolbar is omitted, no FPS cell is shown. **Modals**: while the render guard suppresses full redraws, the last displayed value may freeze; no overlay inside modals is required.

5. **Toggle**: **`UiSettings.ShowRenderFps`** (persisted in appsettings with other UI settings). A **General Settings hub** row toggles the value with **Enter** and persists via existing save flow.

## Consequences

- **Main loop**: Iteration time is dominated by `Redraw` work plus a **cooperative yield** (`Thread.Sleep(0)`), not a fixed ~16 ms sleep. `OnAudioData` feeds analysis only; it does not schedule full renders. WASAPI callback rate may still cap how often FFT/spectrum **analysis** advances, independent of toolbar FPS.
- **Threading**: `AnalysisEngine` synchronizes `ProcessAudio` / `GetSnapshot` and returns snapshot **array copies** so the capture thread and UI thread do not race.
- **CPU / I/O**: Peak load can be higher than fixed ~60 Hz pacing when the host keeps up; diff-based `ViewportCellBuffer` remains important (ADR-0030).
- **Cross-references**: Supersedes the implicit “~20 Hz” as the **design target** for main redraws (ADR-0030 context); performance rules in ADR-0030 remain. Header independence remains per ADR-0038.
- **Tests**: Integration render budget messaging should reflect the **at least ~60 FPS** work budget (single-frame time ≲ ~17 ms on reference paths); unit tests cover FPS rolling-average math on `MainRenderFpsMeter`.

## References

- [`VisualizationOrchestrator`](../../src/AudioAnalyzer.Console/VisualizationOrchestrator.cs) — full render, FPS sampling; `OnAudioData` → `ProcessAudio` only
- [`ApplicationShell`](../../src/AudioAnalyzer.Console/ApplicationShell.cs) — main loop calls `Redraw` each tick; cooperative yield after draw
- [`MainContentToolbarLayout`](../../src/AudioAnalyzer.Console/MainContentToolbarLayout.cs) — toolbar FPS cell
- [`MainRenderFpsMeter`](../../src/AudioAnalyzer.Application/Display/MainRenderFpsMeter.cs) — rolling average
