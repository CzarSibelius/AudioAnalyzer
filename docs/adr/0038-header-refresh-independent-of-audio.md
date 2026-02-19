# ADR-0038: Header refresh independent of audio pipeline

**Status**: Accepted

## Context

Scrolling text in the header (device name, now-playing) only advanced when audio was playing. The frame loop was entirely audio-driven: `ProcessAudio` refreshed the header and rendered the visualizer. WASAPI loopback does not fire `DataAvailable` when no application is playing audio, so header updates stopped when the system was silent.

## Decision

1. **Independent header refresh timer**: A `PeriodicTimer` (50ms) runs in the background, decoupled from the audio capture. On each tick, `AnalysisEngine.RefreshHeaderIfNeeded()` refreshes only the header (device, now-playing, BPM, volume) without re-rendering the visualizer.

2. **Guards**: The timer invokes the refresh only when the render guard passes (modal not open), not in full-screen mode, and not when an overlay is active. Uses the same console lock as `ProcessAudio` and `Redraw` for thread safety.

3. **Lifecycle**: The timer is started when `ApplicationShell.Run()` begins; `CancellationTokenSource` is cancelled and disposed in `Shutdown()`.

4. **Visualizer still audio-driven**: `ProcessAudio` continues to drive the main visualization (FFT, spectrum, layers). The timer only ensures header elements (including scrolling text) animate even when no audio is playing.

## Consequences

- Scrolling text in the header works when using loopback capture with no audio playing.
- Aligns with ADR-0030 (PeriodicTimer for background polling, 20–50ms intervals).
- Minor CPU overhead from periodic header redraws; header cache (per ADR-0030) skips identical writes.
- References: [AnalysisEngine.RefreshHeaderIfNeeded](../../src/AudioAnalyzer.Application/AnalysisEngine.cs), [ApplicationShell.Run](../../src/AudioAnalyzer.Console/ApplicationShell.cs).
