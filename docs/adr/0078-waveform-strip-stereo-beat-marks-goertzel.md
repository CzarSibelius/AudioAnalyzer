# ADR-0078: Waveform strip stereo overview, stored beat marks, and Goertzel bucket coloring

**Status**: Accepted

## Context

[ADR-0077](0077-waveform-overview-snapshot.md) defines a **mono** long-history ring and **8192-bucket** min/max overview plus lightweight band proxies for strip coloring, with an explicit “not in scope” list (Rekordbox-grade per-pixel FFT, locked musical grid, stereo deck strips).

The **Waveform strip** layer needs closer parity with those goals without breaking the **~50 ms** overview rebuild cadence or **GetSnapshot** clone budgets from ADR-0077 and [ADR-0030](0030-performance-priority.md).

## Decision

1. **Stereo overview**: `AnalysisEngine` keeps a **second ring** for the **right** channel (same capacity and write cursor as mono). Overview rebuild fills **parallel** min/max, heuristic band, and Goertzel arrays for left and right. Snapshots expose `WaveformOverview*Right` and Goertzel-right mirrors. Memory for waveform history is **approximately doubled** while capture runs (bounded by the same max-sample cap as ADR-0077).
2. **Stored beat marks**: On each **increment** of `IBeatTimingSource.BeatCount`, the engine appends a bounded ring entry: **monotonic global mono sample index** (`_historyTotalWritten` at append time) and the **beat ordinal** (`BeatCount` after the increment). `FillSnapshot` copies filtered marks into `AudioAnalysisSnapshot` (`WaveformBeatMarkMonoSampleIndex`, `WaveformBeatMarkBeatOrdinal`, `WaveformBeatMarkLength`). The strip maps marks to columns using the same oldest→newest timeline as the overview (`WaveformOverviewOldestMonoSampleIndex`, `WaveformOverviewNewestMonoSampleIndex`, `WaveformOverviewValidSampleCount`). When no marks are present, the strip **falls back** to BPM-spaced grid lines from `CurrentBpm` (legacy behavior).
3. **Goertzel bucket coloring**: For each overview bucket, after min/max aggregation, take up to **256 consecutive samples** from that bucket and compute **Goertzel magnitude** at three fixed frequencies (**150 Hz**, **1.8 kHz**, **6 kHz**, clamped below Nyquist). Results populate `WaveformOverviewBandLowGoertzel` / `Mid` / `High` (and right-channel mirrors). The layer mode `**SpectralGoertzel`** uses these arrays; `**SpectralApprox`** keeps the prior heuristic proxies on `WaveformOverviewBandLow` / `Mid` / `High`. This is **not** a full short-time FFT per column and not Rekordbox RGB fidelity.

## Consequences

- **Snapshot size**: Additional float arrays (Goertzel + right overview) increase per-frame clone cost; remain fixed-length (8192) per series, not proportional to ring size.
- **Beat alignment**: Grid lines reflect **discrete detected/session beats**; spacing can be irregular when tempo drifts. Bar cues use `(beatOrdinal - 1) % BeatsPerBar == 0` with a user **BeatsPerBar** setting (default 4).
- **WaveformStripSettings**: Adds `StereoLayout`, `BeatsPerBar`, `BeatGridOffsetColumns`, and `SpectralGoertzel` color mode; documented in [waveform-strip.md](../visualizers/waveform-strip.md).
- **Tests** cover Goertzel tuning, snapshot fields, beat-mark recording, and column mapping helpers.

## Related

- [ADR-0077](0077-waveform-overview-snapshot.md) — mono ring, overview shape, clone rules
- [ADR-0024](0024-analysissnapshot-frame-context.md) — snapshot vs frame context
- [docs/visualizers/waveform-strip.md](../visualizers/waveform-strip.md)