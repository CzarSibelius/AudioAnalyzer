# ADR-0072: Delta-time display animation (reference 60 Hz)

**Status**: Accepted

## Context

The main display loop can exceed ~60 FPS (ADR-0067). Text layers and toolbar/header scrolling advanced state **once per draw** with fixed increments, so animation speed depended on frame rate.

## Decision

1. **Reference frame rate**: Treat existing tuned numeric increments as **“per reference frame at 60 Hz.”** Do not change preset JSON or user-facing speed fields for compatibility (ADR-0029).

2. **Scaling**: Use `DisplayAnimationTiming.ScaleForReference60(frameDeltaSeconds)` (= `frameDeltaSeconds * 60`) as the multiplier for those increments so that at exactly 60 FPS behavior matches the historical per-draw step.

3. **`VisualizationFrameContext.FrameDeltaSeconds`**: Set by the visualization orchestrator on each full main render (high-resolution timer, clamped to a reasonable maximum, first frame uses `1/60` s). Not produced by `AnalysisEngine`. New frames default it to `0` until the orchestrator assigns it.

4. **`IDisplayFrameClock`**: Header rows use `RenderContext` with `Frame == null`; the console registers a singleton clock updated by the orchestrator each render tick and set to ~`0.05` s for header-only refresh ticks (50 ms timer, ADR-0038).

5. **`TextLayerDrawContext.FrameDeltaSeconds`**: Copied from `ctx.Frame.FrameDeltaSeconds` each layer draw (fallback `1/60` if unset).

6. **UI scrolling**: `IScrollingTextEngine` advances offset by `speedPerReferenceFrame * ScaleForReference60(frameDeltaSeconds)`. `UiSettings.DefaultScrollingSpeed` remains the stored value; documentation describes it as character advance per **reference** frame at 60 Hz.

7. **FallingLetters column rain (former MatrixRain)**: On **Flash** beat, the discrete `colPhase += Random` nudge remains **per draw** (not scaled by `dt`) so column desynchronization stays a discrete per-frame effect; document here as intentional ([ADR-0081](0081-consolidate-matrix-rain-into-falling-letters.md)).

## Consequences

- Animation and scrolling stay visually consistent when FPS varies.
- New layers must use `ctx.FrameDeltaSeconds` (or equivalent) for any continuous motion.
- ADR-0043 updated: `FrameDeltaSeconds` is allowed on `TextLayerDrawContext` as shared frame timing.
- Settings modal and other UI without orchestrator timing set `RenderContext.FrameDeltaSeconds` explicitly.

## References

- [`DisplayAnimationTiming`](../../src/AudioAnalyzer.Application/DisplayAnimationTiming.cs)
- [`VisualizationOrchestrator`](../../src/AudioAnalyzer.Console/VisualizationOrchestrator.cs)
- [`TextLayerDrawContext`](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayerDrawContext.cs)
- [`ScrollingTextEngine`](../../src/AudioAnalyzer.Application/ScrollingTextEngine.cs)
- [ADR-0067](0067-60fps-target-and-render-fps-overlay.md), [ADR-0043](0043-textlayer-state-store.md)
