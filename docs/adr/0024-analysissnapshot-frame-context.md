# ADR-0024: Audio analysis snapshot vs visualization frame context

**Status**: Accepted

## Context

Historically a single `AnalysisSnapshot` type mixed **audio analysis output** (FFT, waveform, volume, beats) with **terminal layout** (`DisplayStartRow`, terminal size) and **render instrumentation** (`FrameDeltaSeconds`, `MeasuredMainRenderFps`, `LayerRenderTimeMs`). The engine already cloned only analysis arrays in `GetSnapshot()`, while the orchestrator mutated the rest each frame—so the type name did not match a single owner.

Earlier reviews (palette removed from the bag, no service-derived data) remain valid; this ADR records the **type split** that makes those boundaries explicit.

## Decision

1. **`AudioAnalysisSnapshot`** ([AudioAnalysisSnapshot.cs](../../src/AudioAnalyzer.Application/Abstractions/AudioAnalysisSnapshot.cs)) holds **only** data produced and cloned by `AnalysisEngine.GetSnapshot()`: volume, BPM / beat fields, FFT bands, waveform, channel peaks. No terminal layout or render-only metrics.

2. **`VisualizationFrameContext`** ([VisualizationFrameContext.cs](../../src/AudioAnalyzer.Application/Abstractions/VisualizationFrameContext.cs)) is the per–main-render **frame bag**: required `Analysis` plus `DisplayStartRow`, `TerminalWidth`, `TerminalHeight`, `FrameDeltaSeconds`, optional `MeasuredMainRenderFps`, optional `LayerRenderTimeMs`. Built by `VisualizationOrchestrator` and passed to `IVisualizationRenderer.Render` and `IVisualizer.Render`.

3. **`RenderContext.Frame`** ([RenderContext.cs](../../src/AudioAnalyzer.Application/Abstractions/RenderContext.cs)) carries an optional `VisualizationFrameContext` for toolbar + visualizer + General settings hub. Header-only renders leave `Frame` null; `IDisplayFrameClock` supplies delta for those rows (ADR-0038, ADR-0072).

4. **Toolbar / palette display** stays off analysis types: palette name and cycling state live on the renderer (e.g. `MainContentContainer`), not on `VisualizationFrameContext`.

5. **No service-derived data** on `AudioAnalysisSnapshot` or frame context for layer content: inject services into layers (ADR-0028).

6. **`BeatSensitivity`** remains on `AudioAnalysisSnapshot` as beat-timing configuration surfaced with analysis (same as before the split).

## Consequences

- Call sites use `VisualizationFrameContext` where the full frame is needed; `AnalysisEngine.GetSnapshot()` returns `AudioAnalysisSnapshot` only.
- UI that needs analysis for palette animation but not full layout (e.g. theme modal) may take `Func<AudioAnalysisSnapshot>` from the engine.
- Settings modal and similar use `IVisualizationOrchestrator.GetFrameForUi()` for merged analysis + cached layer timings (ADR-0073).
- Historical references in older ADRs to “`AnalysisSnapshot`” for **current** code mean **`AudioAnalysisSnapshot`** and/or **`VisualizationFrameContext`** depending on context; superseded historical file paths in audits may still name the old type.

## References

- [ADR-0028](0028-layer-dependency-injection.md), [ADR-0072](0072-delta-time-display-animation.md), [ADR-0073](0073-layer-render-time-overlay.md)
