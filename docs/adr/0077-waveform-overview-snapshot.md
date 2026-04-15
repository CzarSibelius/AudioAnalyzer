# ADR-0077: Decimated waveform overview in snapshot (long internal ring)

**Status**: Accepted

## Context

The UI thread calls `AnalysisEngine.GetSnapshot()` every main render ([ADR-0030](0030-performance-priority.md), [ADR-0067](0067-60fps-target-and-render-fps-overlay.md)). A naive approach that clones **all** mono samples for a long history (for example **60 s at 48 kHz**, ~2.88 million floats, ~11 MB) on every frame would dominate allocation and cache behavior and break the **≥~60 FPS** redraw bar.

The **Oscilloscope** text layer still needs a **short, high-rate** tail (512 samples) so the trace reads as a scope, not a smeared minute-long overview.

## Decision

1. **Internal storage**: `AnalysisEngine` keeps a **mono ring buffer** sized from `AppSettings.MaxAudioHistorySeconds` × the current capture sample rate, with a **hard cap** on sample count (currently **5,000,000** samples) to bound RAM. One mono sample is written per input frame, as before.
2. **Snapshot split** (`AudioAnalysisSnapshot`):
   - **`Waveform` / `WaveformPosition` / `WaveformSize`**: unchanged meaning — **512-sample** scope window (newest samples) for `OscilloscopeLayer` and legacy strip behavior.
   - **Overview**: fixed **8192** buckets rebuilt on the existing **~50 ms** display sync gate. Each bucket stores **min/max** mono samples over its time span plus **lightweight band proxies** (mean absolute, RMS, max step) for **spectral-ish** strip coloring without an FFT per column.
   - **`WaveformOverviewLength`**, **`WaveformOverviewSpanSeconds`**: describe the decimated series for the **Waveform strip** layer time axis.
   - **Build anchor** (`WaveformOverviewBuiltValidSampleCount`, `WaveformOverviewBuiltOldestMonoSampleIndex`, `WaveformOverviewBuiltNewestMonoSampleIndex`): set together with each overview rebuild so the strip can place beat/grid columns on the same timeline as the bucket data (live newest/valid in the snapshot may advance between rebuilds).
3. **Cloning**: `GetSnapshot` clones the **512** waveform floats and **only the filled prefix** of the overview arrays (length `WaveformOverviewLength`), not the full internal ring.
4. **Configuration**: `MaxAudioHistorySeconds` is persisted with app settings (clamped **5–180** in the repository), editable from the General Settings hub, and applied via **`IWaveformHistoryConfigurator.ApplyMaxHistorySeconds`** so the console hub does not depend on `AnalysisEngine` concrete type. Invalid values are clamped; no migration of corrupt files beyond existing backup-and-reset ([ADR-0029](0029-no-settings-migration.md)).

## Consequences

- **Bucket boundaries** in the overview rebuild must use **64-bit** indices when mapping buckets to sample ranges (`bucketIndex * sampleCount` can exceed `int.MaxValue` for multi-million-sample rings); otherwise overview rebuild can throw or corrupt indices.
- **Waveform strip** prefers overview data when `WaveformOverviewLength > 0`; otherwise it falls back to `Waveform` (tests, early startup).
- **Strip time geometry** ([ADR-0079](0079-waveform-strip-fixed-visible-seconds.md), [docs/visualizers/waveform-strip.md](../visualizers/waveform-strip.md)): columns map **linearly** in wall time within the trailing window; **right** = newest samples (“now” for the tail), **left** = older history toward **now − effective visible seconds** — normative wording in ADR-0079 **Time axis (normative)**.
- **Not in scope**: Rekordbox-grade per-pixel FFT RGB, musical grid locked to a master track, stereo deck strips — future work if needed; beat grid in the strip aligns to **app BPM estimate** only (documented in the layer spec).

## Update (current)

- **`IWaveformOverviewRebuildPolicy`**: The console registers a preset-aware policy (`VisualizerSettingsWaveformOverviewRebuildPolicy`) so each ~50 ms gate either **skips** overview work when no enabled **WaveformStrip** layer exists, or **partitions only a trailing mono window** whose sample count is the largest **`FixedVisibleSeconds`** among enabled strips (clamped to the valid mono count) — overview work stays **Θ(window)** ([ADR-0079](0079-waveform-strip-fixed-visible-seconds.md)). Tests use `AnalysisEngine.LastOverviewRebuildPartitionMonoSampleCount` (internal) to assert the partition size. Unit tests that construct `AnalysisEngine` **without** a policy keep **full-ring** rebuild behavior for those tests only.

## Performance follow-ups (deferred)

- **Lock contention**: `RebuildWaveformOverview` still runs inside `ProcessAudio` under `AnalysisEngine`’s sync lock, so a long overview pass can extend the window where `GetSnapshot` blocks the capture thread. **Update:** skipping or windowing reduces typical load; remaining work: incremental bucket updates, a lower rebuild cadence for display-only data, or moving overview work off the audio lock (coordinate with [ADR-0030](0030-performance-priority.md) and main-loop profiling in [docs/agents/testing-and-verification.md](../agents/testing-and-verification.md)).

## Related

- [ADR-0024](0024-analysissnapshot-frame-context.md) — snapshot vs frame context
- [ADR-0061](0061-general-settings-mode.md) — General Settings hub
- [ADR-0078](0078-waveform-strip-stereo-beat-marks-goertzel.md) — stereo rings, beat marks, Goertzel strip coloring
- [docs/visualizers/waveform-strip.md](../visualizers/waveform-strip.md)
