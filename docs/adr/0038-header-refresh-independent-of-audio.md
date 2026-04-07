# ADR-0038: Header refresh independent of audio pipeline

**Status**: Accepted

## Context

Scrolling text in the header (device name, now-playing) only advanced when audio was playing. The frame loop was entirely audio-driven: `ProcessAudio` refreshed the header and rendered the visualizer. WASAPI loopback does not fire `DataAvailable` when no application is playing audio, so header updates stopped when the system was silent.

## Decision

1. **Independent header refresh timer**: A `PeriodicTimer` (50ms) runs in the background, decoupled from the audio capture. On each tick, `VisualizationOrchestrator.RefreshHeaderIfNeeded()` runs `AnalysisEngine.PulseBeatVisualIfDue()` and the header refresh callback only (device, now-playing, BPM, volume) without running a full main-area render.

2. **Guards**: The timer invokes the refresh only when the render guard passes (modal not open), not in full-screen mode, and not when an overlay is active. Uses the same console lock as `Redraw` for thread safety.

3. **Lifecycle**: The timer is started when `ApplicationShell.Run()` begins; `CancellationTokenSource` is cancelled and disposed in `Shutdown()`.

4. **Full main redraw is display-driven**: The shell main loop calls `Redraw` each iteration at display cadence (see ADR-0067). `ProcessAudio` on the capture thread updates analysis only (FFT, spectrum, beat timing). The timer still ensures header-only updates when **no** full render runs (e.g. modal open) and complements the main loop when the system is silent.

## Consequences

- Scrolling text in the header works when using loopback capture with no audio playing.
- Aligns with ADR-0030 (PeriodicTimer for background polling, 20–50ms intervals).
- Minor CPU overhead from periodic header redraws; header cache (per ADR-0030) skips identical writes.
- References: [`VisualizationOrchestrator.RefreshHeaderIfNeeded`](../../src/AudioAnalyzer.Console/VisualizationOrchestrator.cs), [`ApplicationShell.Run`](../../src/AudioAnalyzer.Console/ApplicationShell.cs).
