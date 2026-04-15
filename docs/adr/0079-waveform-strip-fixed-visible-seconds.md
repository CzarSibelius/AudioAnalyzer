# ADR-0079: Waveform strip fixed trailing seconds across strip width

**Status**: Accepted

## Context

The decimated overview ([ADR-0077](0077-waveform-overview-snapshot.md)) must not map an unbounded retained mono ring across a fixed column count: that would change **pixels per wall-second** as the ring fills and would make overview rebuild cost scale with **ring size** every display gate.

The strip therefore always maps a **trailing wall-time window** across the layer width. The engine may retain a longer mono ring (**Max audio history**); `IWaveformOverviewRebuildPolicy` aggregates only the trailing window needed for the strip(s).

## Decision

1. **Layer settings** (`WaveformStripSettings`, Custom JSON on `TextLayerType.WaveformStrip`):
  - `**FixedVisibleSeconds`**: wall seconds mapped across the strip; clamped **1–120**, default **15**, editable in the S modal via reflection ([ADR-0021](0021-textlayer-settings-common-custom.md), [ADR-0025](0025-reflection-based-layer-settings.md)). There is **no** separate “full history” mode.
2. **Rendering** (Visualizer column mapping; engine overview partition):
  - `**WaveformStripVisibleChronology`**: `RingSampleCount` (partition `n` = built valid sample count for the current gate), `WindowSampleCount` (samples mapped across columns, `≤ n`), `OldestVisibleChronologicalIndex` (`0` when the visible window is the whole partition, else `n - WindowSampleCount` for a tail slice narrower than `n` when the ring has not yet filled the requested window).
  - Column `x` maps to chronological index `OldestVisibleChronologicalIndex + x * (WindowSampleCount - 1) / (w - 1)` (integer stepping; same partition lookup via `WaveformOverviewBucketIndex` as `AnalysisEngine`).
  - Beat/grid alignment uses the **same** `WaveformStripVisibleChronology` instance as the waveform draw path.
3. **BPM fallback grid** and **time label** (when enabled) use the **visible** span in seconds: `WaveformOverviewSpanSeconds * WindowSampleCount / RingSampleCount` when the window is a proper tail slice within the partition.

## Time axis (normative)

- The strip uses a **linear** wall-time scale across columns: **constant seconds per column** for the visible window (subject to integer column stepping and overview bucket quantization at the ~50 ms rebuild gate).
- **Rightmost column** = newest captured audio in the current partition (effectively **“now”** for the displayed tail).
- **Leftmost column** = oldest audio **in that visible window**, i.e. approximately **now minus the effective visible span** in wall time. The effective span matches `**FixedVisibleSeconds`** once the ring has enough history; it can be **shorter** until the ring fills (see **UX** under **Consequences** below).

## Consequences

- **Memory / CPU**: Still one **8192-bucket** overview per rebuild when any **WaveformStrip** is enabled; `AnalysisEngine` aggregates only the **trailing mono window** (largest `FixedVisibleSeconds` among enabled strips, clamped to valid mono count) via `**IWaveformOverviewRebuildPolicy`**, so CPU scales with that window, not the full retained ring ([ADR-0077](0077-waveform-overview-snapshot.md)). When no WaveformStrip is enabled, overview rebuild is **skipped** for that gate.
- **UX**: Shorter `FixedVisibleSeconds` zooms in on the recent past; left edge is the oldest sample **in the visible window**, not the global oldest sample in the ring. Beats older than that tail do not draw on the strip.
- **Tests**: `WaveformStripVisibleChronology`; `AnalysisEngineOverviewRebuildTests` / `VisualizerSettingsWaveformOverviewRebuildPolicyTests` for partition size.

## History

Earlier revisions exposed `**OverviewTimeMode`** (`FullHistory` | `FixedSecondsVisible`). **FullHistory** and that setting were removed: the strip always behaves as fixed trailing seconds above.

## Related

- [ADR-0077](0077-waveform-overview-snapshot.md) — overview snapshot and ring
- [ADR-0078](0078-waveform-strip-stereo-beat-marks-goertzel.md) — strip stereo, beats, Goertzel
- [docs/visualizers/waveform-strip.md](../visualizers/waveform-strip.md)